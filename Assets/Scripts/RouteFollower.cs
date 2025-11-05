using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Waypoint到達時に発火するイベント
/// </summary>
[System.Serializable]
public class WaypointEvent
{
    [Tooltip("イベントを発火するWaypoint番号（0始まり）")]
    public int waypointIndex;

    [Header("Camera Look At Settings")]
    [Tooltip("注視対象Transform（設定時に自動的にカメラ注視演出を実行）")]
    public Transform lookAtTarget;

    [Tooltip("注視継続時間（秒）")]
    public float lookDuration = 3.0f;

    [Header("Additional Events")]
    [Tooltip("追加の演出イベント（パーティクル、音等）無い場合は空でOK")]
    public UnityEvent onReached;
}

/// <summary>
/// ルートモード
/// </summary>
public enum RouteMode
{
    OneWay,     // 一方通行（終端で停止）
    Loop,       // 循環（終端から始点へループ）
    PingPong    // 往復（終端で反転）
}

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

    [Tooltip("各Waypoint到達時に発火するイベント（演出設定用）")]
    [SerializeField] private WaypointEvent[] waypointEvents;

    [Tooltip("ルートモード（OneWay=一方通行、Loop=循環、PingPong=往復）")]
    [SerializeField] private RouteMode routeMode = RouteMode.OneWay;

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

    [Header("Camera Effects")]
    [Tooltip("カメラ演出制御（注視演出用）")]
    [SerializeField] private CameraViewController cameraViewController;

    // 現在向かっているウェイポイントのインデックス
    private int currentWaypointIndex = 0;

    // 前進中フラグ（腕振り検知状態と連動）
    private bool isMoving = false;

    // PingPongモード用の逆走フラグ
    private bool isReversing = false;

    // カメラ演出実行中フラグ（注視→復帰が完全終了するまでtrue）
    private bool isPerformingEvent = false;

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
        // 意図: イベント実行中は入力を受け付けず、演出完了まで待機
        if (isMoving && !isPerformingEvent && HasNextWaypoint())
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

        // CameraViewControllerの自動検索（Inspector未設定時）
        if (cameraViewController == null)
        {
            cameraViewController = FindObjectOfType<CameraViewController>();

            if (cameraViewController == null)
            {
                Debug.LogWarning("[RouteFollower] CameraViewControllerが見つかりません。カメラ注視演出を使用する場合は設定してください。");
            }
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
    /// 意図: ルート終端での配列外アクセスを防ぐ（PingPongモード対応）
    /// </summary>
    private bool HasNextWaypoint()
    {
        if (isReversing)
        {
            return currentWaypointIndex >= 0;
        }
        else
        {
            return currentWaypointIndex < waypoints.Length;
        }
    }

    /// <summary>
    /// 現在のウェイポイントへ向かって移動
    /// 意図: 一定速度での滑らかな移動を実現、到達時に次へ進む
    /// </summary>
    private void MoveTowardsCurrentWaypoint()
    {
        GameObject targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 targetPosition = targetWaypoint.transform.position;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        // 意図: 完全一致(==)は浮動小数点誤差で永遠に到達できないため許容範囲を設定
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            AdvanceToNextWaypoint();
        }
    }

    /// <summary>
    /// 次のウェイポイントへインデックスを進める
    /// 意図: ルート上の進行を管理、演出の自動実行、終端到達時は完了処理（Loop/PingPong対応）
    /// </summary>
    private void AdvanceToNextWaypoint()
    {
        if (waypointEvents != null)
        {
            foreach (var evt in waypointEvents)
            {
                if (evt != null && evt.waypointIndex == currentWaypointIndex)
                {
                    if (evt.lookAtTarget != null)
                    {
                        LookAtWithPause(evt.lookAtTarget, evt.lookDuration);
                    }

                    evt.onReached?.Invoke();
                }
            }
        }

        // インデックス更新（PingPongモード対応）
        if (isReversing)
        {
            currentWaypointIndex--;
        }
        else
        {
            currentWaypointIndex++;
        }

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
    /// 意図: ルートモードに応じた終端処理（OneWay=停止、Loop=ループ、PingPong=反転）
    /// </summary>
    private void OnRouteCompleted()
    {
        switch (routeMode)
        {
            case RouteMode.OneWay:
                isMoving = false;
                Debug.Log("[RouteFollower] Route completed!");
                break;

            case RouteMode.Loop:
                currentWaypointIndex = 0;
                Debug.Log("[RouteFollower] Route looping to start...");
                break;

            case RouteMode.PingPong:
                isReversing = !isReversing;
                if (isReversing)
                {
                    // 前進終了→逆走開始（最後から2番目へ）
                    currentWaypointIndex = waypoints.Length - 2;
                }
                else
                {
                    // 逆走終了→前進開始（最初から2番目へ）
                    currentWaypointIndex = 1;
                }
                Debug.Log($"[RouteFollower] Route reversing... Direction={( isReversing ? "Backward" : "Forward" )}");
                break;
        }
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
    /// 意図: リトライ機能の実装を容易にする（PingPongモード対応）
    /// </summary>
    public void ResetRoute()
    {
        currentWaypointIndex = 0;
        isMoving = false;
        isReversing = false;
    }

    /// <summary>
    /// 移動を一時停止
    /// 意図: 演出中の停止に使用（Tell, Don't Ask原則）
    /// </summary>
    private void PauseMovement()
    {
        isMoving = false;
        Debug.Log("[RouteFollower] Movement paused.");
    }

    /// <summary>
    /// 移動を再開（演出完了後の再開に使用）
    /// 意図: 腕振り検知状態を確認してから再開（Tell, Don't Ask原則）
    /// </summary>
    public void ResumeMovement()
    {
        if (armSwingDetector != null)
        {
            isMoving = armSwingDetector.IsWalking;
            Debug.Log($"[RouteFollower] Movement resumed. IsWalking={armSwingDetector.IsWalking}");
        }
        else
        {
            isMoving = true;
            Debug.Log("[RouteFollower] Movement resumed (no ArmSwingDetector check).");
        }
    }

    /// <summary>
    /// カメラ注視演出を実行（移動停止→注視→カメラ復帰→自動再開を一括処理）
    /// 意図: カプセル化により複雑な処理を隠蔽、WaypointEventから自動呼び出し
    /// イベント完全終了まで入力を受け付けない（isPerformingEvent制御）
    /// </summary>
    private void LookAtWithPause(Transform target, float duration)
    {
        if (cameraViewController == null)
        {
            Debug.LogError("[RouteFollower] CameraViewControllerが未設定です。カメラ注視演出を使用できません。");
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("[RouteFollower] LookAt target is null!");
            return;
        }

        // イベント開始：入力受付停止
        isPerformingEvent = true;
        PauseMovement();

        // 注視演出後のコールバック：カメラを次のWaypointへの向きに復帰→移動再開
        cameraViewController.LookAt(target, duration, () => {
            // 次のWaypointが存在する場合、カメラを移動方向に復帰
            if (HasNextWaypoint())
            {
                Quaternion nextRotation = DetermineTargetRotation(waypoints[currentWaypointIndex]);
                cameraViewController.RestoreRotation(nextRotation, 1.0f, () => {
                    // イベント完全終了：入力受付再開→移動再開
                    isPerformingEvent = false;
                    ResumeMovement();
                });
            }
            else
            {
                // 次のWaypointがない場合（OneWayモード終端）は、そのまま再開
                isPerformingEvent = false;
                ResumeMovement();
            }
        });

        Debug.Log($"[RouteFollower] LookAtWithPause started: Target={target.name}, Duration={duration}s");
    }
}
