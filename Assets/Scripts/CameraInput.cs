using UnityEngine;

/// <summary>
/// WebCamTextureでカメラ入力を取得し、Texture2Dに変換するコンポーネント
/// </summary>
public class CameraInput : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("使用するカメラデバイスのインデックス（0 = デフォルト）")]
    public int cameraIndex = 0;

    [Tooltip("カメラ解像度の幅")]
    public int requestedWidth = 640;

    [Tooltip("カメラ解像度の高さ")]
    public int requestedHeight = 640;

    [Tooltip("カメラのFPS")]
    public int requestedFPS = 30;

    [Header("Output")]
    [Tooltip("現在のカメラフレーム")]
    public Texture2D currentFrame;

    private WebCamTexture webCamTexture;
    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;
    public int Width => webCamTexture != null ? webCamTexture.width : 0;
    public int Height => webCamTexture != null ? webCamTexture.height : 0;

    void Start()
    {
        InitializeCamera();
    }

    void InitializeCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("CameraInput: カメラデバイスが見つかりません");
            return;
        }

        if (cameraIndex >= devices.Length)
        {
            Debug.LogWarning($"CameraInput: カメラインデックス {cameraIndex} が範囲外です。デフォルトカメラを使用します");
            cameraIndex = 0;
        }

        string deviceName = devices[cameraIndex].name;
        Debug.Log($"CameraInput: カメラ起動中... デバイス: {deviceName}");

        webCamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFPS);
        webCamTexture.Play();

        // カメラが起動するまで少し待つ
        StartCoroutine(WaitForCameraStart());
    }

    System.Collections.IEnumerator WaitForCameraStart()
    {
        // カメラが起動するまで最大5秒待機
        float timeout = 5f;
        float elapsed = 0f;

        while (!webCamTexture.isPlaying && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (webCamTexture.isPlaying)
        {
            // Texture2Dを実際のカメラ解像度で作成
            currentFrame = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
            isInitialized = true;
            Debug.Log($"CameraInput: カメラ起動成功 解像度: {webCamTexture.width}x{webCamTexture.height}");
        }
        else
        {
            Debug.LogError("CameraInput: カメラの起動に失敗しました");
        }
    }

    void Update()
    {
        if (!isInitialized || !webCamTexture.isPlaying)
            return;

        // WebCamTextureからTexture2Dにコピー
        if (webCamTexture.didUpdateThisFrame)
        {
            currentFrame.SetPixels(webCamTexture.GetPixels());
            currentFrame.Apply();
        }
    }

    void OnDestroy()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }

        if (currentFrame != null)
        {
            Destroy(currentFrame);
        }
    }

    /// <summary>
    /// 現在のフレームを取得
    /// </summary>
    public Texture2D GetCurrentFrame()
    {
        return currentFrame;
    }

    /// <summary>
    /// カメラが更新されたか
    /// </summary>
    public bool DidUpdateThisFrame()
    {
        return webCamTexture != null && webCamTexture.didUpdateThisFrame;
    }
}
