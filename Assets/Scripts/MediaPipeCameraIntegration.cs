using System.Collections;
using UnityEngine;
using Mediapipe.Unity.Sample;

/// <summary>
/// MediaPipeとCameraInputの統合管理
/// 責任: カメラ入力の共有化、YOLO + MediaPipeの共存を実現
/// </summary>
[DefaultExecutionOrder(100)] // Bootstrap/CameraInputより後に実行
public class MediaPipeCameraIntegration : MonoBehaviour
{
    public static MediaPipeCameraIntegration Instance { get; private set; }
    public static bool IsReady { get; private set; } = false;

    private CameraInput cameraInput;
    private SharedCameraImageSource sharedSource;
    private Bootstrap bootstrap;

    private void Awake()
    {
        // Singleton初期化
        if (Instance == null)
        {
            Instance = this;
            // 親からの独立: ルートGameObjectに移動してDontDestroyOnLoadを機能させる
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeSharedCamera());
    }

    /// <summary>
    /// 共有カメラソースを初期化
    /// 意図: Bootstrap完了後にImageSourceをSharedCameraImageSourceに置き換え
    /// </summary>
    private IEnumerator InitializeSharedCamera()
    {
        // CameraInput Singletonを待機（最大5秒）
        float timeout = 5f;
        while (CameraInput.Instance == null && timeout > 0)
        {
            yield return new WaitForSeconds(0.1f);
            timeout -= 0.1f;
        }

        cameraInput = CameraInput.Instance;
        if (cameraInput == null)
        {
            Debug.LogError("[MediaPipeCameraIntegration] CameraInput.Instance not found! Please add CameraInput to the scene.");
            yield break;
        }

        // Bootstrap動的生成を待機（最大10秒）
        Debug.Log("[MediaPipeCameraIntegration] Waiting for Bootstrap...");
        timeout = 10f;
        while (bootstrap == null && timeout > 0)
        {
            bootstrap = FindFirstObjectByType<Bootstrap>();
            if (bootstrap == null)
            {
                yield return new WaitForSeconds(0.2f);
                timeout -= 0.2f;
            }
        }

        if (bootstrap == null)
        {
            Debug.LogError("[MediaPipeCameraIntegration] Bootstrap not found after 10 seconds!");
            yield break;
        }

        // Bootstrap完了を待機
        Debug.Log("[MediaPipeCameraIntegration] Waiting for Bootstrap to finish...");
        yield return new WaitUntil(() => bootstrap.isFinished);

        // CameraInput初期化を待機
        Debug.Log("[MediaPipeCameraIntegration] Waiting for CameraInput to initialize...");
        yield return new WaitUntil(() => cameraInput.IsInitialized);

        // SharedCameraImageSourceを作成
        sharedSource = new SharedCameraImageSource(cameraInput);
        Debug.Log("[MediaPipeCameraIntegration] SharedCameraImageSource created.");

        // ImageSourceProviderをリフレクション経由で上書き（private setterを強制的に呼び出す）
        var imageSourceProperty = typeof(ImageSourceProvider).GetProperty("ImageSource");
        if (imageSourceProperty != null)
        {
            var setMethod = imageSourceProperty.GetSetMethod(nonPublic: true); // private setterを取得
            if (setMethod != null)
            {
                setMethod.Invoke(null, new object[] { sharedSource });
                Debug.Log("[MediaPipeCameraIntegration] ImageSourceProvider.ImageSource overwritten with SharedCameraImageSource.");
            }
            else
            {
                Debug.LogError("[MediaPipeCameraIntegration] Failed to get setter for ImageSourceProvider.ImageSource.");
                yield break;
            }
        }
        else
        {
            Debug.LogError("[MediaPipeCameraIntegration] ImageSource property not found on ImageSourceProvider.");
            yield break;
        }

        // SharedCameraImageSourceを初期化
        yield return sharedSource.Play();

        if (!sharedSource.isPrepared)
        {
            Debug.LogError("[MediaPipeCameraIntegration] Failed to initialize SharedCameraImageSource.");
            yield break;
        }

        IsReady = true;
        Debug.Log("[MediaPipeCameraIntegration] Camera integration completed successfully!");
        Debug.Log($"[MediaPipeCameraIntegration] Resolution: {sharedSource.resolution.width}x{sharedSource.resolution.height}");
    }

    /// <summary>
    /// エディタテスト用にstatic変数をリセット
    /// 意図: PlayMode終了時やシーン単体テスト時の初期化
    /// </summary>
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        IsReady = false;
        Debug.Log("[MediaPipeCameraIntegration] Static variables reset for editor.");
    }
#endif
}
