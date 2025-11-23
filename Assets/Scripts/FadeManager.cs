using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 画面フェード演出を管理するシングルトン
/// 責任: 画面暗転・明転演出のみに専念
/// 用途: シーン遷移、カットシーン、演出等、アプリ全体で使用可能
/// </summary>
public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    [Header("Fade Settings")]
    [Tooltip("フェード時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;

    // Fade用UI
    private Canvas fadeCanvas;
    private Image fadeImage;

    private void Awake()
    {
        // Singleton初期化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFadeUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Fade用UIを初期化
    /// 意図: Canvas + Imageを自動生成、全画面黒パネルを用意
    /// </summary>
    private void InitializeFadeUI()
    {
        // Canvas作成
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform, false);

        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999; // 最前面

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Image作成（黒い全画面パネル）
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // 初期状態は透明

        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Debug.Log("[FadeManager] Fade UI initialized.");
    }

    /// <summary>
    /// フェードアウト（画面を暗く）
    /// 意図: 滑らかな補間で画面を黒に変化
    /// </summary>
    /// <param name="duration">フェード時間（秒）。省略時はデフォルト値使用</param>
    public IEnumerator FadeOut(float duration = -1f)
    {
        if (fadeImage == null)
        {
            Debug.LogError("[FadeManager] Fade image is null!");
            yield break;
        }

        // duration省略時はデフォルト値使用
        if (duration < 0f)
        {
            duration = fadeDuration;
        }

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
    }

    /// <summary>
    /// フェードイン（画面を明るく）
    /// 意図: 滑らかな補間で画面を透明に変化
    /// </summary>
    /// <param name="duration">フェード時間（秒）。省略時はデフォルト値使用</param>
    public IEnumerator FadeIn(float duration = -1f)
    {
        if (fadeImage == null)
        {
            Debug.LogError("[FadeManager] Fade image is null!");
            yield break;
        }

        // duration省略時はデフォルト値使用
        if (duration < 0f)
        {
            duration = fadeDuration;
        }

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

    /// <summary>
    /// エディタテスト用にインスタンスをリセット
    /// 意図: PlayMode終了時やシーン単体テスト時の初期化
    /// </summary>
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        Instance = null;
        Debug.Log("[FadeManager] Instance reset for editor.");
    }
#endif
}
