using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shiftキー入力でカメラ映像の表示/非表示を切り替える
/// 責任: カメラUI表示制御のみに専念（単一責任原則）
/// </summary>
public class CameraViewToggle : MonoBehaviour
{
    private RawImage cameraRawImage;

    /// <summary>
    /// 同じGameObject内のRawImageコンポーネントを自動取得
    /// 意図: Inspector設定不要で参照を確保
    /// </summary>
    private void Awake()
    {
        cameraRawImage = GetComponent<RawImage>();

        if (cameraRawImage == null)
        {
            Debug.LogError("[CameraViewToggle] RawImage component not found on this GameObject!");
        }
    }

    /// <summary>
    /// Shiftキー入力を監視してカメラ表示をトグル
    /// </summary>
    private void Update()
    {
        // Left Shift または Right Shift でトグル
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            ToggleCameraView();
        }
    }

    /// <summary>
    /// カメラ映像の表示/非表示を切り替え
    /// 意図: RawImageコンポーネントのenable/disableで制御
    /// </summary>
    private void ToggleCameraView()
    {
        if (cameraRawImage != null)
        {
            cameraRawImage.enabled = !cameraRawImage.enabled;
            Debug.Log($"[CameraViewToggle] Camera view: {(cameraRawImage.enabled ? "Visible" : "Hidden")}");
        }
    }
}
