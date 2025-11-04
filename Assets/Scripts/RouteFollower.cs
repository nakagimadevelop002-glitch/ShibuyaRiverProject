using UnityEngine;

/// <summary>
/// 腕振り検知に応じて設定されたルートを自動的にたどる移動システム
/// リングフィットアドベンチャー方式：腕を振ると前進、停止すると待機
/// ウェイポイントの回転設定に応じてカメラ向きを制御（自動/手動両対応）
/// </summary>
public class RouteFollower : MonoBehaviour
{
    [Header("Route Settings")]
    [Tooltip("ルート上の通過点を順番に配置（位置＝移動先、回転＝カメラ向き）")]
    [SerializeField] private GameObject[] waypoints;

    [Header("Movement Settings")]
    [Tooltip("前進速度（m/s）")]
    [SerializeField] private float moveSpeed = 2.0f;

    [Tooltip("カメラ回転速度（滑らかさ）")]
    [SerializeField] private float rotationSpeed = 5.0f;

    [Header("Camera Settings")]
    [Tooltip("プレイヤーカメラ（視点制御対象）")]
    [SerializeField] private Transform playerCamera;

    [Tooltip("ウェイポイント回転未設定時に移動方向へ自動回転するか")]
    [SerializeField] private bool autoRotateToMoveDirection = true;

    [Header("Detection")]
    [Tooltip("腕振り検知システム（前進/停止トリガー）")]
    [SerializeField] private ArmSwingDetector armSwingDetector;

    // 現在向かっているウェイポイントのインデックス
    private int currentWaypointIndex = 0;

    // 前進中フラグ（腕振り検知状態と連動）
    private bool isMoving = false;

    private void Start()
    {
        ValidateSetup();
        SubscribeToArmSwingEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromArmSwingEvents();
    }

    private void Update()
    {
        if (isMoving && HasNextWaypoint())
        {
            MoveTowardsCurrentWaypoint();
            RotateTowardsTarget();
        }
    }

    /// <summary>
    /// 初期設定の妥当性を検証
    /// 意図: 実行時エラーを事前に防ぎ、設定ミスを早期発見
    /// </summary>
    private void ValidateSetup()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("[RouteFollower] Waypoints配列が空です。ルートを設定してください。");
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("[RouteFollower] Player Cameraが未設定です。視点制御が無効化されます。");
        }

        if (armSwingDetector == null)
        {
            Debug.LogError("[RouteFollower] ArmSwingDetectorが未設定です。移動制御ができません。");
        }
    }

    /// <summary>
    /// 腕振り検知イベントを購読
    /// 意図: イベント駆動による疎結合な設計（Tell, Don't Ask原則）
    /// </summary>
    private void SubscribeToArmSwingEvents()
    {
        if (armSwingDetector != null)
        {
            armSwingDetector.onWalkingDetected.AddListener(StartMoving);
            armSwingDetector.onWalkingStopped.AddListener(StopMoving);
        }
    }

    /// <summary>
    /// 腕振り検知イベントの購読解除
    /// 意図: メモリリーク防止（OnDestroyでの確実なクリーンアップ）
    /// </summary>
    private void UnsubscribeFromArmSwingEvents()
    {
        if (armSwingDetector != null)
        {
            armSwingDetector.onWalkingDetected.RemoveListener(StartMoving);
            armSwingDetector.onWalkingStopped.RemoveListener(StopMoving);
        }
    }

    /// <summary>
    /// 腕振り検知時：前進を開始
    /// 意図: ユーザーの運動を移動に変換（リングフィット方式）
    /// </summary>
    private void StartMoving()
    {
        isMoving = true;
    }

    /// <summary>
    /// 腕振り停止時：その場で待機
    /// 意図: 運動停止時は移動も停止（直感的な操作感）
    /// </summary>
    private void StopMoving()
    {
        isMoving = false;
    }

    /// <summary>
    /// 次のウェイポイントが存在するか確認
    /// 意図: ルート終端での配列外アクセスを防ぐ
    /// </summary>
    private bool HasNextWaypoint()
    {
        return currentWaypointIndex < waypoints.Length;
    }

    /// <summary>
    /// 現在のウェイポイントへ向かって移動
    /// 意図: 一定速度での滑らかな移動を実現、到達時に次へ進む
    /// </summary>
    private void MoveTowardsCurrentWaypoint()
    {
        GameObject targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 targetPosition = targetWaypoint.transform.position;

        // Y座標は固定（高さ維持、ユーザーが手動設定）
        targetPosition.y = transform.position.y;

        // 目標地点へ移動（MoveTowardsで一定速度）
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        // 到達判定（0.1m以内）
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            AdvanceToNextWaypoint();
        }
    }

    /// <summary>
    /// 次のウェイポイントへインデックスを進める
    /// 意図: ルート上の進行を管理、終端到達時は完了処理
    /// </summary>
    private void AdvanceToNextWaypoint()
    {
        currentWaypointIndex++;

        if (!HasNextWaypoint())
        {
            OnRouteCompleted();
        }
    }

    /// <summary>
    /// カメラを目標方向へ回転
    /// 意図: ウェイポイント設定に応じた柔軟な視点制御（自動/手動両対応）
    /// </summary>
    private void RotateTowardsTarget()
    {
        if (playerCamera == null || !HasNextWaypoint()) return;

        GameObject targetWaypoint = waypoints[currentWaypointIndex];
        Quaternion targetRotation = DetermineTargetRotation(targetWaypoint);

        // 回転が決定できた場合のみ実行
        if (targetRotation != Quaternion.identity || IsWaypointRotationSet(targetWaypoint))
        {
            ApplySmoothRotation(targetRotation);
        }
    }

    /// <summary>
    /// ウェイポイント設定に基づいて目標回転を決定
    /// 意図: 回転設定の有無で自動/手動を切り替える柔軟性
    /// </summary>
    private Quaternion DetermineTargetRotation(GameObject targetWaypoint)
    {
        // ウェイポイントに回転が設定されている場合はその向きへ
        if (IsWaypointRotationSet(targetWaypoint))
        {
            return targetWaypoint.transform.rotation;
        }

        // 設定がない場合は移動方向へ自動回転（有効時）
        if (autoRotateToMoveDirection)
        {
            return CalculateRotationTowardsWaypoint(targetWaypoint);
        }

        // 自動回転無効の場合は現在の回転を維持
        return playerCamera.rotation;
    }

    /// <summary>
    /// ウェイポイント方向への回転を計算
    /// 意図: 移動方向への自然な視点誘導
    /// </summary>
    private Quaternion CalculateRotationTowardsWaypoint(GameObject targetWaypoint)
    {
        Vector3 direction = targetWaypoint.transform.position - transform.position;
        direction.y = 0; // 水平方向のみ（上下は見ない）

        if (direction == Vector3.zero)
        {
            return playerCamera.rotation; // 方向がない場合は現状維持
        }

        return Quaternion.LookRotation(direction);
    }

    /// <summary>
    /// 滑らかにカメラを回転
    /// 意図: 急激な視点変化を避け、VR酔いを防止
    /// </summary>
    private void ApplySmoothRotation(Quaternion targetRotation)
    {
        playerCamera.rotation = Quaternion.Slerp(
            playerCamera.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// ウェイポイントに回転設定があるか判定
    /// 意図: デフォルト回転（0,0,0）と明示的な設定を区別
    /// </summary>
    private bool IsWaypointRotationSet(GameObject waypoint)
    {
        // 回転がデフォルト値（Quaternion.identity）でない場合、設定ありと判定
        return waypoint.transform.rotation != Quaternion.identity;
    }

    /// <summary>
    /// ルート完走時の処理
    /// 意図: ルート終了時の拡張ポイント（今後UnityEvent等を追加可能）
    /// </summary>
    private void OnRouteCompleted()
    {
        isMoving = false;
        Debug.Log("[RouteFollower] Route completed!");
        // 今後の拡張: UnityEvent等でルート完走イベントを発火し、次のシーンへ遷移等
    }

    /// <summary>
    /// 現在の進行状況を取得（デバッグ・UI表示用）
    /// </summary>
    public float GetProgress()
    {
        if (waypoints == null || waypoints.Length == 0) return 0f;
        return (float)currentWaypointIndex / waypoints.Length;
    }

    /// <summary>
    /// ルートをリセットして最初から再開
    /// 意図: リトライ機能の実装を容易にする
    /// </summary>
    public void ResetRoute()
    {
        currentWaypointIndex = 0;
        isMoving = false;
    }
}
