using UnityEngine;

/// <summary>
/// ArmSwingDetectorからの歩行検知を受け取り、Playerキャラクターを前進させる
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerWalkController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.0f; // 前進速度（m/s）
    [SerializeField] private float acceleration = 5.0f; // 加速度（滑らかな開始・停止）

    [Header("References")]
    [SerializeField] private ArmSwingDetector armSwingDetector;

    // パフォーマンス最適化: CharacterControllerをキャッシュ
    private CharacterController characterController;

    // 現在の移動速度（加速・減速の滑らかな遷移用）
    private float currentSpeed = 0f;

    private void Start()
    {
        // 初回のみGetComponent実行、以降はキャッシュを使用
        characterController = GetComponent<CharacterController>();

        // ArmSwingDetectorの自動検索（Inspector未設定時）
        if (armSwingDetector == null)
        {
            armSwingDetector = FindObjectOfType<ArmSwingDetector>();

            if (armSwingDetector == null)
            {
                Debug.LogError("[PlayerWalkController] ArmSwingDetector not found in scene!");
                return;
            }
        }

        // イベント購読: 歩行検知・停止時のコールバック設定
        armSwingDetector.onWalkingDetected.AddListener(OnWalkingDetected);
        armSwingDetector.onWalkingStopped.AddListener(OnWalkingStopped);
    }

    private void OnDestroy()
    {
        // メモリリーク防止: イベント購読解除
        if (armSwingDetector != null)
        {
            armSwingDetector.onWalkingDetected.RemoveListener(OnWalkingDetected);
            armSwingDetector.onWalkingStopped.RemoveListener(OnWalkingStopped);
        }
    }

    private void Update()
    {
        UpdateMovement();
    }

    /// <summary>
    /// 移動処理: 歩行状態に応じて速度を調整し、前進させる
    /// </summary>
    private void UpdateMovement()
    {
        // 目標速度を決定（歩行中か停止中か）
        float targetSpeed = armSwingDetector.IsWalking ? moveSpeed : 0f;

        // 滑らかな加速・減速
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        // 前進方向の移動ベクトル計算
        Vector3 moveDirection = transform.forward * currentSpeed;

        // 重力適用（地面に接地させるため）
        if (!characterController.isGrounded)
        {
            moveDirection.y -= 9.81f; // 重力加速度
        }

        // CharacterControllerで移動実行
        characterController.Move(moveDirection * Time.deltaTime);
    }

    /// <summary>
    /// 歩行検知時のコールバック
    /// </summary>
    private void OnWalkingDetected()
    {
        Debug.Log("[PlayerWalkController] Player started walking.");
    }

    /// <summary>
    /// 歩行停止時のコールバック
    /// </summary>
    private void OnWalkingStopped()
    {
        Debug.Log("[PlayerWalkController] Player stopped walking.");
    }

    /// <summary>
    /// 現在の移動速度を取得（デバッグ用）
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
}
