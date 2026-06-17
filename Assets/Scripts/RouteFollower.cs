using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Waypoint到達時に発火するイベント
/// </summary>
[System.Serializable]
public class WaypointEvent
{
    [Header("イベントを発火するWaypoint番号")]
    public int waypointIndex;

    [Header("注視対象Transform（停止して注視 または 移動しながら注視）")]
    public Transform lookAtTarget;

    [Header("停止して注視する場合の継続時間（秒）")]
    public float lookDuration = 3.0f;

    [Header("移動しながら注視する場合はチェックON")]
    public bool lookWhileMoving = false;

    [Header("移動しながら注視を終了するWaypoint番号（-1=無効）")]
    public int lookUntilWaypointIndex = -1;

    [Header("補間注視・視線開始地点（橋の根元など）最優先")]
    public Transform lookAtStart;

    [Header("補間注視・視線終了地点（橋の終端など）両方設定で有効")]
    public Transform lookAtEnd;

    [Header("追加イベント（パーティクル、音など）")]
    public UnityEvent onReached;

    [Header("シーン遷移を実行する場合はチェックON")]
    public bool enableSceneTransition = false;

    [Header("遷移先シーン名")]
    public string targetSceneName = "";
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
    [Header("ルート上の通過点（Waypoint）を順番に配置")]
    [SerializeField] private GameObject[] waypoints;

    [Header("各Waypoint到達時のイベント設定（カメラ注視、補間注視、シーン遷移など）")]
    [SerializeField] private WaypointEvent[] waypointEvents;

    [Header("ルートモード（OneWay=一方通行 / Loop=循環 / PingPong=往復）")]
    [SerializeField] private RouteMode routeMode = RouteMode.OneWay;

    [Header("プレイヤーの前進速度（m/s）推奨：2.0〜5.0")]
    [SerializeField] private float moveSpeed = 2.0f;

    [Header("カメラ回転速度（大きいほど素早く回転）推奨：3.0〜10.0")]
    [SerializeField] private float rotationSpeed = 5.0f;

    [Header("プレイヤーカメラ（Main Cameraを指定）")]
    [SerializeField] private Transform playerCamera;

    [Header("Waypoint回転未設定時に進行方向へ自動回転するか")]
    [SerializeField] private bool autoRotateToMoveDirection = true;

    [Header("腕振り検知システム（ArmSwingDetector）")]
    [SerializeField] private ArmSwingDetector armSwingDetector;

    [Header("カメラ演出制御（CameraViewController）")]
    [SerializeField] private CameraViewController cameraViewController;

    [Header("最終地点での分岐選択（割当時はシーン自動遷移せずUIで選択）")]
    [SerializeField] private RouteBranchChooser endBranchChooser;

    [Header("自動前進モード（テスト用）本番はOFF")]
    [SerializeField] private bool autoMoveEnabled = false;

    [Header("寄り道・移動開始点（NewWayPoint）※移動2点＋注視2点が全て割当済みのときルート中央で寄り道発動")]
    [SerializeField] private Transform detourMoveStart;

    [Header("寄り道・移動終了点（NewWayPoint (1)）")]
    [SerializeField] private Transform detourMoveEnd;

    [Header("寄り道・注視開始点（NewLookPoint）")]
    [SerializeField] private Transform detourLookStart;

    [Header("寄り道・注視終了点（NewLookPoint (1)）")]
    [SerializeField] private Transform detourLookEnd;

    [Header("寄り道区間（NewWayPoint間）専用の移動速度。小さいほどゆっくり。本筋moveSpeedとは独立")]
    [SerializeField] private float detourMoveSpeed = 0.5f;

    [Header("寄り道から中間地点へ復帰した後、通常ルート再開までの待機秒数")]
    [SerializeField] private float detourReturnWaitSeconds = 2.0f;

    [Header("寄り道を発動するルート全長に対する進捗割合（0.5=空間的な真ん中）")]
    [Range(0f, 1f)]
    [SerializeField] private float detourTriggerProgress = 0.5f;

    // ルート全長（始点から終点までのWayPoint間距離の総和）
    private float routeTotalLength = 0f;

    // これまでの累積移動距離（寄り道発動判定用）
    private float traveledDistance = 0f;

    // 寄り道を実行済みか（1ルート中1回のみ発動）
    private bool detourDone = false;

    // 現在向かっているウェイポイントのインデックス
    private int currentWaypointIndex = 0;

    // 前進中フラグ（腕振り検知状態と連動）
    private bool isMoving = false;

    // PingPongモード用の逆走フラグ
    private bool isReversing = false;

    // カメラ演出実行中フラグ（注視→復帰が完全終了するまでtrue）
    private bool isPerformingEvent = false;

    // 継続的注視の対象（移動しながら注視）
    private Transform continuousLookAtTarget = null;

    // 継続的注視の終了WaypointIndex
    private int continuousLookAtEndIndex = -1;

    // 補間注視用の状態管理（移動しながら2点間を補間注視）
    private Transform interpolationLookAtStart = null;
    private Transform interpolationLookAtEnd = null;
    private Vector3 interpolationStartPosition = Vector3.zero;
    private Vector3 interpolationEndPosition = Vector3.zero;

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
        // フェード中はプレイヤーの動きを停止
        if (FadeManager.IsFading)
        {
            return;
        }

        // "Jump"ボタン（デフォルト: スペースキー）でautoMoveEnabledをトグル
        if (Input.GetButtonDown("Jump"))
        {
            autoMoveEnabled = !autoMoveEnabled;

            // 自動前進を無効化した時は、腕振り検知状態を反映
            if (!autoMoveEnabled && armSwingDetector != null)
            {
                isMoving = armSwingDetector.IsWalking;
            }

            Debug.Log($"[RouteFollower] AutoMove toggled: {(autoMoveEnabled ? "Enabled" : "Disabled")}");
        }

        // テスト用自動前進モード（腕振り検知を無視して強制的に前進）
        if (autoMoveEnabled)
        {
            isMoving = true;
        }

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
            cameraViewController = FindFirstObjectByType<CameraViewController>();

            if (cameraViewController == null)
            {
                Debug.LogWarning("[RouteFollower] CameraViewControllerが見つかりません。カメラ注視演出を使用する場合は設定してください。");
            }
        }

        // ルート全長を算出（隣接WayPoint間距離の総和）。寄り道を空間的な真ん中で発動させるため
        routeTotalLength = 0f;
        if (waypoints != null)
        {
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                {
                    routeTotalLength += Vector3.Distance(
                        waypoints[i].transform.position,
                        waypoints[i + 1].transform.position);
                }
            }
        }
        Debug.Log($"[RouteFollower] Route total length = {routeTotalLength:F1}, detour triggers at {detourTriggerProgress * 100f:F0}% (≒{routeTotalLength * detourTriggerProgress:F1})");
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

        Vector3 beforePosition = transform.position;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        // 累積移動距離を更新し、ルート全長の指定割合（空間的な真ん中）に到達したら寄り道を1回だけ発動
        traveledDistance += Vector3.Distance(beforePosition, transform.position);
        if (!detourDone && !isReversing && IsDetourConfigured() &&
            routeTotalLength > 0f &&
            traveledDistance >= detourTriggerProgress * routeTotalLength)
        {
            detourDone = true;
            StartDetour();
            return; // 寄り道発動中は以降の到達判定をスキップ（isPerformingEventで停止）
        }

        // 意図: 完全一致(==)は浮動小数点誤差で永遠に到達できないため許容範囲を設定
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            // 継続的注視の終了判定（WayPoint到達時）
            CheckContinuousLookAtEnd();

            AdvanceToNextWaypoint();
        }
    }

    /// <summary>
    /// 寄り道の4点（移動2＋注視2）が全て割当済みか
    /// </summary>
    private bool IsDetourConfigured()
    {
        return detourMoveStart != null && detourMoveEnd != null &&
               detourLookStart != null && detourLookEnd != null;
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
                    // 全ての演出後に実行する処理をまとめる
                    System.Action executeAfterEffects = () =>
                    {
                        // 追加イベント実行
                        evt.onReached?.Invoke();

                        // シーン遷移処理（最後に実行）
                        if (evt.enableSceneTransition && !string.IsNullOrEmpty(evt.targetSceneName))
                        {
                            if (endBranchChooser != null)
                            {
                                // 分岐選択あり: 自動遷移せず移動を止めてUIで選択させる
                                // まっすぐ先 = このイベントの通常遷移先シーン
                                isMoving = false;
                                isPerformingEvent = true;
                                endBranchChooser.ShowChoice(evt.targetSceneName);
                            }
                            else
                            {
                                ScenePortal portal = new ScenePortal();
                                portal.Enter(evt.targetSceneName);
                            }
                        }
                    };

                    // LookAt演出がある場合の処理分岐
                    // 補間注視モード（最優先）
                    if (evt.lookAtStart != null && evt.lookAtEnd != null)
                    {
                        // 次のWaypointを取得（PingPongモード対応）
                        int nextWaypointIndex = isReversing ? currentWaypointIndex - 1 : currentWaypointIndex + 1;

                        // 配列外アクセス防止チェック
                        if (nextWaypointIndex >= 0 && nextWaypointIndex < waypoints.Length)
                        {
                            GameObject nextWaypoint = waypoints[nextWaypointIndex];
                            StartInterpolatedLookAt(
                                evt.lookAtStart,
                                evt.lookAtEnd,
                                transform.position,  // 現在位置（到達したWaypoint）
                                nextWaypoint.transform.position  // 次のWaypoint位置
                            );
                        }
                        else
                        {
                            Debug.LogWarning("[RouteFollower] 次のWaypointが存在しないため補間注視をスキップ");
                        }

                        // 移動は停止せず、追加イベントのみ実行
                        executeAfterEffects();
                    }
                    else if (evt.lookAtTarget != null)
                    {
                        // 既存の1点注視モード
                        // 移動しながら注視モード（新機能）
                        if (evt.lookWhileMoving)
                        {
                            StartContinuousLookAt(evt.lookAtTarget, evt.lookUntilWaypointIndex);
                            // 移動は停止せず、追加イベントのみ実行
                            executeAfterEffects();
                        }
                        else
                        {
                            // 既存の停止して注視モード
                            LookAtWithPause(evt.lookAtTarget, evt.lookDuration, executeAfterEffects);
                        }
                    }
                    else
                    {
                        // LookAt演出がない場合は即座に実行
                        executeAfterEffects();
                    }
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
    /// 拡張: 補間注視モード → 継続的注視モード → 通常回転の優先順位で処理
    /// </summary>
    private void RotateTowardsTarget()
    {
        if (playerCamera == null || !HasNextWaypoint()) return;

        Quaternion targetRotation;

        // 補間注視モードが有効な場合、最優先で処理
        Vector3? interpolatedPosition = UpdateInterpolatedLookAt();
        if (interpolatedPosition.HasValue)
        {
            Vector3 direction = interpolatedPosition.Value - playerCamera.position;
            if (direction != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(direction);
                ApplySmoothRotation(targetRotation);
            }
            return; // 補間注視中は他の回転処理をスキップ
        }

        // 継続的注視モードが有効な場合、注視対象を優先
        if (continuousLookAtTarget != null)
        {
            Vector3 direction = continuousLookAtTarget.position - playerCamera.position;
            if (direction != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(direction);
                ApplySmoothRotation(targetRotation);
            }
            return; // 注視中は通常の回転処理をスキップ
        }

        // 通常のWaypoint回転制御
        GameObject targetWaypoint = waypoints[currentWaypointIndex];
        targetRotation = DetermineTargetRotation(targetWaypoint);

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
    /// 意図: 移動方向への自然な視点誘導（上下方向も含む完全な方向）
    /// </summary>
    private Quaternion CalculateRotationTowardsWaypoint(GameObject targetWaypoint)
    {
        Vector3 direction = targetWaypoint.transform.position - transform.position;

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
                // Debug.Log("[RouteFollower] Route completed!");
                break;

            case RouteMode.Loop:
                currentWaypointIndex = 0;
                // Debug.Log("[RouteFollower] Route looping to start...");
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
                // Debug.Log($"[RouteFollower] Route reversing... Direction={( isReversing ? "Backward" : "Forward" )}");
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
        traveledDistance = 0f;
        detourDone = false;
    }

    /// <summary>
    /// 移動を一時停止
    /// 意図: 演出中の停止に使用（Tell, Don't Ask原則）
    /// </summary>
    private void PauseMovement()
    {
        isMoving = false;
        // Debug.Log("[RouteFollower] Movement paused.");
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
            // Debug.Log($"[RouteFollower] Movement resumed. IsWalking={armSwingDetector.IsWalking}");
        }
        else
        {
            isMoving = true;
            // Debug.Log("[RouteFollower] Movement resumed (no ArmSwingDetector check).");
        }
    }

    /// <summary>
    /// カメラ注視演出を実行（移動停止→注視→カメラ復帰→自動再開を一括処理）
    /// 意図: カプセル化により複雑な処理を隠蔽、WaypointEventから自動呼び出し
    /// イベント完全終了まで入力を受け付けない（isPerformingEvent制御）
    /// </summary>
    /// <param name="target">注視対象</param>
    /// <param name="duration">注視時間</param>
    /// <param name="onComplete">演出完了後に実行するコールバック</param>
    private void LookAtWithPause(Transform target, float duration, System.Action onComplete = null)
    {
        if (cameraViewController == null)
        {
            Debug.LogError("[RouteFollower] CameraViewControllerが未設定です。カメラ注視演出を使用できません。");
            onComplete?.Invoke();  // 演出できなくても後続処理は実行
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("[RouteFollower] LookAt target is null!");
            onComplete?.Invoke();  // 演出できなくても後続処理は実行
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

                    // 演出完了後の追加処理を実行
                    onComplete?.Invoke();
                });
            }
            else
            {
                // 次のWaypointがない場合（OneWayモード終端）は、そのまま再開
                isPerformingEvent = false;
                ResumeMovement();

                // 演出完了後の追加処理を実行
                onComplete?.Invoke();
            }
        });

        // Debug.Log($"[RouteFollower] LookAtWithPause started: Target={target.name}, Duration={duration}s");
    }

    /// <summary>
    /// 継続的注視モードを開始（移動しながら注視）
    /// 意図: 指定WayPoint間で対象を注視し続ける（移動は停止しない）
    /// </summary>
    /// <param name="target">注視対象</param>
    /// <param name="endWaypointIndex">注視終了WaypointIndex</param>
    private void StartContinuousLookAt(Transform target, int endWaypointIndex)
    {
        if (target == null)
        {
            Debug.LogWarning("[RouteFollower] Continuous LookAt target is null!");
            return;
        }

        continuousLookAtTarget = target;
        continuousLookAtEndIndex = endWaypointIndex;

        Debug.Log($"[RouteFollower] Continuous LookAt started: Target={target.name}, EndIndex={endWaypointIndex}");
    }

    /// <summary>
    /// 継続的注視モードを終了
    /// 意図: 指定Waypointに到達したら注視を解除
    /// </summary>
    private void StopContinuousLookAt()
    {
        if (continuousLookAtTarget != null)
        {
            Debug.Log($"[RouteFollower] Continuous LookAt stopped at WaypointIndex={currentWaypointIndex}");
            continuousLookAtTarget = null;
            continuousLookAtEndIndex = -1;
        }
    }

    /// <summary>
    /// 補間注視モードを開始（移動しながら2点間を補間注視）
    /// 意図: 移動進捗に応じてlookAtStartからlookAtEndへ視線を滑らかに移動
    /// </summary>
    /// <param name="lookAtStart">注視開始地点</param>
    /// <param name="lookAtEnd">注視終了地点</param>
    /// <param name="startPosition">移動開始位置（現在のプレイヤー位置）</param>
    /// <param name="endPosition">移動終了位置（次のWaypoint位置）</param>
    private void StartInterpolatedLookAt(Transform lookAtStart, Transform lookAtEnd, Vector3 startPosition, Vector3 endPosition)
    {
        if (lookAtStart == null || lookAtEnd == null)
        {
            Debug.LogWarning("[RouteFollower] Interpolated LookAt targets are null!");
            return;
        }

        interpolationLookAtStart = lookAtStart;
        interpolationLookAtEnd = lookAtEnd;
        interpolationStartPosition = startPosition;
        interpolationEndPosition = endPosition;

        Debug.Log($"[RouteFollower] Interpolated LookAt started: Start={lookAtStart.name}, End={lookAtEnd.name}");
    }

    /// <summary>
    /// 補間注視の更新（毎フレーム呼び出し）
    /// 意図: 移動進捗に応じて目標視線位置を連続的に更新し、滑らかな視線移動を実現
    /// </summary>
    /// <returns>補間注視用の目標視線位置（nullの場合は補間注視モードが無効）</returns>
    private Vector3? UpdateInterpolatedLookAt()
    {
        // 補間注視モードが無効な場合はnullを返す
        if (interpolationLookAtStart == null || interpolationLookAtEnd == null)
        {
            return null;
        }

        // 移動進捗率を計算（0.0〜1.0）
        // 注意: Waypointに沿った移動ではなく、開始地点→終了地点の直線距離ベースで計算
        // Playerが曲線移動する場合も、視線補間は直線的に進行する（自然な視線移動を実現）
        float totalDistance = Vector3.Distance(interpolationStartPosition, interpolationEndPosition);
        if (totalDistance < 0.01f)
        {
            // 距離がほぼゼロの場合は終了地点を返す
            return interpolationLookAtEnd.position;
        }

        float currentDistance = Vector3.Distance(interpolationStartPosition, transform.position);
        float progress = Mathf.Clamp01(currentDistance / totalDistance);

        // 進捗率に応じて注視対象位置を補間
        Vector3 interpolatedLookAtPosition = Vector3.Lerp(
            interpolationLookAtStart.position,
            interpolationLookAtEnd.position,
            progress
        );

        return interpolatedLookAtPosition;
    }

    /// <summary>
    /// 補間注視モードを終了
    /// 意図: WayPoint到達時に補間注視をクリア
    /// </summary>
    private void StopInterpolatedLookAt()
    {
        if (interpolationLookAtStart != null || interpolationLookAtEnd != null)
        {
            Debug.Log($"[RouteFollower] Interpolated LookAt stopped at WaypointIndex={currentWaypointIndex}");
            interpolationLookAtStart = null;
            interpolationLookAtEnd = null;
            interpolationStartPosition = Vector3.zero;
            interpolationEndPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// 継続的注視の終了判定
    /// 意図: 現在のWaypointIndexが終了Indexに到達したか確認
    /// 拡張: 補間注視モードの終了判定も追加（Waypoint到達時に自動終了）
    /// </summary>
    private void CheckContinuousLookAtEnd()
    {
        // 継続的注視モードの終了判定
        if (continuousLookAtTarget != null &&
            continuousLookAtEndIndex >= 0 &&
            currentWaypointIndex >= continuousLookAtEndIndex)
        {
            StopContinuousLookAt();
        }

        // 補間注視モードの終了判定（Waypoint到達時）
        if (interpolationLookAtStart != null && interpolationLookAtEnd != null)
        {
            StopInterpolatedLookAt();
        }
    }

    /// <summary>
    /// 寄り道演出を開始（移動停止→別経路へ離脱→2点間を移動注視→元の中間地点へ復帰→通常ルート再開）
    /// 意図: LookAtWithPauseと同様、isPerformingEventで通常移動を停止し、完了後に再開
    /// </summary>
    private void StartDetour(System.Action onComplete = null)
    {
        if (playerCamera == null)
        {
            Debug.LogError("[RouteFollower] Player Cameraが未設定です。寄り道演出を使用できません。");
            onComplete?.Invoke();
            return;
        }

        // 寄り道開始時のリグ位置とカメラの向きを厳密に記録し、復帰時に同じ視界へ完全復元する
        // （WayPoint座標へ戻す＋出口方向へ向け直すと、寄り道前に見えていた対象が画角外になるため）
        Vector3 startRigPos = transform.position;
        Quaternion startCamRot = playerCamera.rotation;

        // イベント開始：入力受付停止
        isPerformingEvent = true;
        PauseMovement();

        StartCoroutine(DetourCoroutine(startRigPos, startCamRot, () =>
        {
            // イベント完全終了：入力受付再開→移動再開
            isPerformingEvent = false;
            ResumeMovement();
            onComplete?.Invoke();
        }));

        Debug.Log($"[RouteFollower] Detour started at WaypointIndex={currentWaypointIndex}");
    }

    /// <summary>
    /// 寄り道のコルーチン実装（3フェーズ：離脱→2点間移動注視→復帰）
    /// 進行は本筋同様 isMoving（腕振り）連動でゲート。フェード中は停止
    /// </summary>
    private IEnumerator DetourCoroutine(Vector3 startRigPos, Quaternion startCamRot, System.Action onDone)
    {
        // フェーズ1: 移動開始点へ一瞬で切替（カット）。当たり判定無視の貫通移動を避けるため補間しない
        // 視線も即座に注視開始点へ向ける
        transform.position = detourMoveStart.position;
        SnapFaceTowards(detourLookStart.position);
        yield return null; // カットを1フレーム反映

        // フェーズ2: 移動開始点 → 移動終了点を直進しつつ、注視を開始点→終了点で進捗補間
        yield return MoveRigInterpolated(detourMoveStart.position, detourMoveEnd.position, detourLookStart, detourLookEnd);

        // フェーズ3: 寄り道前のリグ位置・カメラ向きへ厳密に復元（同じ視界へ戻す）
        transform.position = startRigPos;
        playerCamera.rotation = startCamRot;

        // 復帰後すぐに動かず、少し待ってから通常ルートを再開する
        yield return new WaitForSeconds(detourReturnWaitSeconds);

        Debug.Log("[RouteFollower] Detour completed.");
        onDone?.Invoke();
    }

    /// <summary>
    /// カメラを指定ワールド座標へ即座に向ける（カット切替用、補間なし）
    /// </summary>
    private void SnapFaceTowards(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - playerCamera.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            playerCamera.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// リグを2点間で直進させながら、注視対象を開始→終了で進捗補間
    /// </summary>
    private IEnumerator MoveRigInterpolated(Vector3 from, Vector3 to, Transform lookFrom, Transform lookTo)
    {
        float totalDistance = Vector3.Distance(from, to);

        while (Vector3.Distance(transform.position, to) > 0.1f)
        {
            if (!FadeManager.IsFading && (isMoving || autoMoveEnabled))
            {
                transform.position = Vector3.MoveTowards(transform.position, to, detourMoveSpeed * Time.deltaTime);
            }

            float progress = totalDistance < 0.01f
                ? 1f
                : Mathf.Clamp01(Vector3.Distance(from, transform.position) / totalDistance);

            Vector3 lookPosition = Vector3.Lerp(lookFrom.position, lookTo.position, progress);
            FaceTowards(lookPosition);
            yield return null;
        }
    }

    /// <summary>
    /// カメラを指定ワールド座標へ滑らかに向ける（rotationSpeedでSlerp）
    /// </summary>
    private void FaceTowards(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - playerCamera.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            playerCamera.rotation = Quaternion.Slerp(playerCamera.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
