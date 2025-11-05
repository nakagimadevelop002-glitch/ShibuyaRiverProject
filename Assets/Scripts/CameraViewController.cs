using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// カメラの注視演出を担当するコンポーネント
/// 責任: 指定地点を指定秒数だけ見る演出の実行
/// オブジェクト指向原則: Tell, Don't Ask - 「これを見て」と命令を受け取り、完了を通知する
/// </summary>
public class CameraViewController : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("制御対象のカメラTransform")]
    [SerializeField] private Transform controlledCamera;

    [Tooltip("カメラ回転速度（滑らかさ）")]
    [SerializeField] private float rotationSpeed = 5.0f;

    // 注視演出完了時のコールバック
    private System.Action onCompleted;

    // 現在実行中のコルーチン（重複実行防止）
    private Coroutine currentLookAtCoroutine;

    private void Start()
    {
        // 自動検索（Inspector未設定時）
        if (controlledCamera == null)
        {
            controlledCamera = Camera.main?.transform;

            if (controlledCamera == null)
            {
                Debug.LogError("[CameraViewController] Main Camera not found! Please assign controlledCamera in Inspector.");
            }
        }
    }

    /// <summary>
    /// 指定地点を指定秒数だけ注視する（パラメータ版）
    /// Tell: このメソッドに「これを見て」と命令するだけ
    /// </summary>
    /// <param name="target">注視対象のTransform</param>
    /// <param name="duration">注視継続時間（秒）</param>
    /// <param name="onComplete">演出完了時のコールバック</param>
    public void LookAt(Transform target, float duration, System.Action onComplete = null)
    {
        if (target == null)
        {
            Debug.LogWarning("[CameraViewController] LookAt target is null!");
            return;
        }

        if (controlledCamera == null)
        {
            Debug.LogError("[CameraViewController] Controlled camera is not assigned!");
            return;
        }

        // 既存のコルーチンを停止（重複実行防止）
        if (currentLookAtCoroutine != null)
        {
            StopCoroutine(currentLookAtCoroutine);
        }

        onCompleted = onComplete;
        currentLookAtCoroutine = StartCoroutine(LookAtCoroutine(target, duration));
    }

    /// <summary>
    /// 注視演出のコルーチン実装
    /// 意図: 滑らかな補間と正確な時間制御
    /// </summary>
    private IEnumerator LookAtCoroutine(Transform target, float duration)
    {
        float elapsed = 0f;

        // 目標回転を計算（注視対象への方向）
        Vector3 direction = target.position - controlledCamera.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Debug.Log($"[CameraViewController] Starting LookAt: Target={target.name}, Duration={duration}s");

        // 指定秒数だけ目標方向へ滑らか補間
        while (elapsed < duration)
        {
            // Slerpで滑らかに回転
            controlledCamera.rotation = Quaternion.Slerp(
                controlledCamera.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[CameraViewController] LookAt completed.");

        // 完了通知（RouteFollowerの移動再開等に使用）
        onCompleted?.Invoke();

        currentLookAtCoroutine = null;
    }

    /// <summary>
    /// 注視演出を即座に中断
    /// 意図: 緊急停止やシーン遷移時の安全なクリーンアップ
    /// </summary>
    public void StopLookAt()
    {
        if (currentLookAtCoroutine != null)
        {
            StopCoroutine(currentLookAtCoroutine);
            currentLookAtCoroutine = null;
            Debug.Log("[CameraViewController] LookAt stopped.");
        }
    }

    /// <summary>
    /// 現在注視演出実行中か確認（デバッグ用）
    /// </summary>
    public bool IsLookingAt()
    {
        return currentLookAtCoroutine != null;
    }

    /// <summary>
    /// カメラを指定回転に復帰
    /// 意図: 注視演出後、移動方向へスムーズに復帰（「見上げたまま」問題の解決）
    /// </summary>
    /// <param name="targetRotation">復帰先の回転</param>
    /// <param name="duration">復帰にかける時間（秒）</param>
    /// <param name="onComplete">復帰完了時のコールバック</param>
    public void RestoreRotation(Quaternion targetRotation, float duration, System.Action onComplete)
    {
        if (controlledCamera == null)
        {
            Debug.LogError("[CameraViewController] Controlled camera is not assigned!");
            return;
        }

        // 既存のコルーチンを停止
        if (currentLookAtCoroutine != null)
        {
            StopCoroutine(currentLookAtCoroutine);
        }

        onCompleted = onComplete;
        currentLookAtCoroutine = StartCoroutine(RestoreRotationCoroutine(targetRotation, duration));
    }

    /// <summary>
    /// カメラ復帰のコルーチン実装
    /// 意図: 滑らかな補間で自然な復帰
    /// </summary>
    private IEnumerator RestoreRotationCoroutine(Quaternion targetRotation, float duration)
    {
        float elapsed = 0f;
        Quaternion startRotation = controlledCamera.rotation;

        Debug.Log("[CameraViewController] Starting rotation restore...");

        while (elapsed < duration)
        {
            controlledCamera.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                elapsed / duration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        controlledCamera.rotation = targetRotation;

        Debug.Log("[CameraViewController] Rotation restore completed.");

        // 完了通知
        onCompleted?.Invoke();

        currentLookAtCoroutine = null;
    }
}
