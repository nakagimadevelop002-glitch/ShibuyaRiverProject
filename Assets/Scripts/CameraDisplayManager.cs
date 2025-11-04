using UnityEngine;

/// <summary>
/// MediaPipe画面を右下小窓に配置するマネージャー
/// AutoFit無効化 + Screen.Resize()でMediaPipe標準の方法でサイズ変更
/// </summary>
public class CameraDisplayManager : MonoBehaviour
{
    [Header("MediaPipe Screen Settings")]
    [Tooltip("MediaPipe画面サイズ（右下小窓）")]
    [SerializeField] private Vector2 screenSize = new Vector2(320, 240);

    private GameObject mediaPipeScreen;
    private MonoBehaviour screenComponent;
    private MonoBehaviour autoFitComponent;

    private void Start()
    {
        HideMediaPipeScreen();

        // MediaPipe初期化完了を待ってからサイズ再設定
        StartCoroutine(ApplyResizeAfterMediaPipeInitialization());
    }

    /// <summary>
    /// MediaPipe初期化完了後にResize()を再実行
    /// 意図: MediaPipeがStart()後にサイズを上書きするため、初期化完了を待つ
    /// </summary>
    private System.Collections.IEnumerator ApplyResizeAfterMediaPipeInitialization()
    {
        // 2秒待機（MediaPipeの初期化完了を待つ）
        yield return new WaitForSeconds(2f);

        if (screenComponent != null)
        {
            var resizeMethod = screenComponent.GetType().GetMethod("Resize");
            if (resizeMethod != null)
            {
                resizeMethod.Invoke(screenComponent, new object[] { (int)screenSize.x, (int)screenSize.y });
                Debug.Log($"[CameraDisplayManager] MediaPipe初期化後に再度Screen.Resize({screenSize.x}, {screenSize.y})を実行しました。");
            }
        }
    }


    /// <summary>
    /// MediaPipeのカメラ映像を小窓表示に変更
    /// 意図: AutoFit無効化 + Screen.Resize()使用でMediaPipe標準の方法でサイズ変更
    /// </summary>
    private void HideMediaPipeScreen()
    {
        // "Annotatable Screen"を検索
        GameObject annotatableScreen = GameObject.Find("Annotatable Screen");

        if (annotatableScreen != null)
        {
            mediaPipeScreen = annotatableScreen;

            // 1. AutoFitコンポーネントを無効化（毎フレームの自動調整を停止）
            var autoFit = mediaPipeScreen.GetComponent(System.Type.GetType("Mediapipe.Unity.AutoFit, Assembly-CSharp"));
            if (autoFit != null)
            {
                autoFitComponent = autoFit as MonoBehaviour;
                if (autoFitComponent != null)
                {
                    autoFitComponent.enabled = false;
                    Debug.Log("[CameraDisplayManager] AutoFitを無効化しました。");
                }
            }

            // 2. Screen.Resize()でサイズ変更（MediaPipe標準の方法）
            var screen = mediaPipeScreen.GetComponentInChildren(System.Type.GetType("Mediapipe.Unity.Screen, Assembly-CSharp"));
            if (screen != null)
            {
                screenComponent = screen as MonoBehaviour;
                if (screenComponent != null)
                {
                    var resizeMethod = screenComponent.GetType().GetMethod("Resize");
                    if (resizeMethod != null)
                    {
                        resizeMethod.Invoke(screenComponent, new object[] { (int)screenSize.x, (int)screenSize.y });
                        Debug.Log($"[CameraDisplayManager] Screen.Resize({screenSize.x}, {screenSize.y})を実行しました。");
                    }
                }
            }

            // 3. RectTransformで位置を右下に配置
            RectTransform rectTransform = annotatableScreen.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(1, 0);
                rectTransform.anchorMax = new Vector2(1, 0);
                rectTransform.pivot = new Vector2(1, 0);
                rectTransform.anchoredPosition = new Vector2(-10, 10);

                Debug.Log("[CameraDisplayManager] Annotatable Screenを右下に配置しました。");
            }
        }
        else
        {
            Debug.LogWarning("[CameraDisplayManager] Annotatable Screenが見つかりませんでした。");
        }
    }

}
