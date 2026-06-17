using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 画面フェード演出を管理するコンポーネント
/// 責任: 画面暗転・明転演出のみ（シーン遷移はScenePortalの責任）
/// 設計方針: 各シーンに配置、DontDestroyOnLoad不使用、シーン開始時に自動フェードイン
/// </summary>
public class FadeManager : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("フェードイン開始前の待機時間（秒）")]
    [SerializeField] private float fadeInDelay = 0f;

    [Tooltip("フェードアウト開始前の待機時間（秒）")]
    [SerializeField] private float fadeOutDelay = 0f;

    [Tooltip("実際のフェード演出時間（秒）")]
    [SerializeField] private float fadeDuration = 2.0f;

    [Tooltip("シーン開始時に自動的にフェードインするか")]
    [SerializeField] private bool autoFadeInOnStart = true;

    [Header("UI References")]
    [Tooltip("フェード用Image（シーンに配置済みのものを参照）")]
    [SerializeField] private Image fadeImage;

    [Header("Game Events")]
    [Tooltip("FadeIn待機完了時に発火するGameEvent（任意）")]
    [SerializeField] private GameEvent onFadeInDelayComplete;

    [Tooltip("FadeOut待機完了時に発火するGameEvent（任意）")]
    [SerializeField] private GameEvent onFadeOutDelayComplete;

    /// <summary>
    /// フェード中かどうか（外部から参照可能）
    /// </summary>
    public static bool IsFading { get; private set; } = false;

    private void Awake()
    {
        // fadeImageが未設定の場合は自動生成
        if (fadeImage == null)
        {
            InitializeFadeUI();
        }

        // フェード画像はクリックを遮らないようにする
        // （全画面ImageはraycastTarget=trueだと透明でも最前面で全UIクリックをブロックするため）
        if (fadeImage != null)
        {
            fadeImage.raycastTarget = false;
        }
    }

    private void Start()
    {
        // シーン開始時に自動フェードイン
        if (autoFadeInOnStart && fadeImage != null)
        {
            // 初期状態を完全暗転に設定
            SetFadeBlack();
            // フェードイン開始
            StartCoroutine(FadeIn());
        }
    }

    /// <summary>
    /// Fade用UIを初期化（自動生成）
    /// 意図: fadeImage未設定時、Canvas + Imageを自動生成して全画面黒パネルを用意
    /// </summary>
    private void InitializeFadeUI()
    {
        // Canvas作成
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999; // 最前面

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Image作成（黒い全画面パネル）
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // 初期状態は透明

        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; // left, bottom
        rt.offsetMax = Vector2.zero; // right, top

        // Debug.Log("[FadeManager] Fade UI auto-generated.");
    }

    /// <summary>
    /// フェードアウトしてコールバック実行
    /// 意図: フェード完了後の処理を呼び出し側に委譲（責任分離）
    /// </summary>
    /// <param name="onComplete">フェード完了時に実行するコールバック</param>
    /// <param name="delayOverride">待機時間（秒）。-1でデフォルト値使用</param>
    /// <param name="durationOverride">フェード時間（秒）。-1でデフォルト値使用</param>
    public void FadeOutWithCallback(System.Action onComplete, float delayOverride = -1f, float durationOverride = -1f)
    {
        StartCoroutine(FadeOutWithCallbackCoroutine(onComplete, delayOverride, durationOverride));
    }

    private IEnumerator FadeOutWithCallbackCoroutine(System.Action onComplete, float delayOverride, float durationOverride)
    {
        // フェード中は重複実行を防止
        if (IsFading)
        {
            Debug.LogWarning("[FadeManager] Already fading. Ignoring request.");
            yield break;
        }

        // フェードアウト完了まで待機
        yield return StartCoroutine(FadeOut(delayOverride, durationOverride));

        // コールバック実行（シーン遷移などの処理は呼び出し側の責任）
        onComplete?.Invoke();
    }

    /// <summary>
    /// フェードアウト（画面を暗く）
    /// 意図: 待機時間後、滑らかな補間で画面を黒に変化
    /// </summary>
    /// <param name="delayOverride">待機時間（秒）。-1でデフォルト値使用</param>
    /// <param name="durationOverride">フェード時間（秒）。-1でデフォルト値使用</param>
    public IEnumerator FadeOut(float delayOverride = -1f, float durationOverride = -1f)
    {
        if (fadeImage == null)
        {
            Debug.LogError("[FadeManager] Fade image is not assigned!");
            yield break;
        }

        IsFading = true;

        // delay省略時はデフォルト値使用（フェードアウト用）
        float delay = delayOverride < 0f ? fadeOutDelay : delayOverride;
        // duration省略時はデフォルト値使用
        float duration = durationOverride < 0f ? fadeDuration : durationOverride;

        // 待機時間
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // 待機完了時にGameEvent発火
        onFadeOutDelayComplete?.Raise();

        // 実際のフェード演出
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            fadeImage.color = color;
            yield return null;
        }

        // 完全に不透明
        color.a = 1f;
        fadeImage.color = color;

        // フェードアウト完了時はIsFadingをtrueのまま維持（シーン遷移中）
    }

    /// <summary>
    /// フェードイン（画面を明るく）
    /// 意図: 待機時間後、滑らかな補間で画面を透明に変化
    /// </summary>
    /// <param name="delayOverride">待機時間（秒）。-1でデフォルト値使用</param>
    /// <param name="durationOverride">フェード時間（秒）。-1でデフォルト値使用</param>
    public IEnumerator FadeIn(float delayOverride = -1f, float durationOverride = -1f)
    {
        if (fadeImage == null)
        {
            Debug.LogError("[FadeManager] Fade image is not assigned!");
            yield break;
        }

        IsFading = true;

        // delay省略時はデフォルト値使用（フェードイン用）
        float delay = delayOverride < 0f ? fadeInDelay : delayOverride;
        // duration省略時はデフォルト値使用
        float duration = durationOverride < 0f ? fadeDuration : durationOverride;

        // 待機時間
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // 待機完了時にGameEvent発火
        onFadeInDelayComplete?.Raise();

        // 実際のフェード演出
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = 1f - Mathf.Clamp01(elapsed / duration);
            fadeImage.color = color;
            yield return null;
        }

        // 完全に透明
        color.a = 0f;
        fadeImage.color = color;

        // フェードイン完了 - プレイヤー操作再開可能
        IsFading = false;
    }

    /// <summary>
    /// 即座に画面を暗転（演出なし）
    /// 意図: シーン開始時の初期状態設定等で使用
    /// </summary>
    public void SetFadeBlack()
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 1f;
            fadeImage.color = color;
        }
    }

    /// <summary>
    /// 即座に画面を透明化（演出なし）
    /// 意図: デバッグ用、または強制クリア
    /// </summary>
    public void SetFadeClear()
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }
    }
}
