using UnityEngine;

/// <summary>
/// ルート最終地点での分岐選択を担当するコンポーネント
/// 責任: 分岐UIの表示と、選択結果に応じたシーン遷移の実行
/// 「まっすぐ進む」=通常の遷移先 / 「脇道」=新宿御苑シーン
/// </summary>
public class RouteBranchChooser : MonoBehaviour
{
    [Header("分岐選択UIのルート（初期は非アクティブ）")]
    [SerializeField] private Transform branchUIRoot;

    [Header("脇道（新宿御苑）の遷移先シーン名")]
    [SerializeField] private string detourSceneName = "ShinjukuGyoenMap";

    [Header("カメラ（手挙げ）での選択を有効化")]
    [SerializeField] private bool enableHandRaiseSelection = true;

    [Tooltip("手首が肩よりこの値以上(正規化y)上にあれば『挙げた』と判定")]
    [SerializeField] private float raiseThreshold = 0.05f;

    [Tooltip("挙げ続けて確定するまでの保持秒数（誤爆防止）")]
    [SerializeField] private float holdTime = 0.7f;

    [Tooltip("左右マッピングを入れ替える（カメラ映像が鏡像で逆になる場合）")]
    [SerializeField] private bool swapLeftRight = false;

    [Header("Poseランドマークindex（MediaPipe Pose）")]
    [SerializeField] private int leftShoulderIndex = 11;
    [SerializeField] private int rightShoulderIndex = 12;
    [SerializeField] private int leftWristIndex = 15;
    [SerializeField] private int rightWristIndex = 16;

    // まっすぐ進む場合の遷移先（RouteFollowerが最終WayPointのtargetSceneNameを渡す）
    private string straightSceneName;

    // 分岐UI表示中で選択待ちか
    private bool awaitingChoice = false;
    private float rightRaisedTime = 0f;
    private float leftRaisedTime = 0f;

    /// <summary>
    /// 分岐UIを表示する（RouteFollowerが最終地点到達時に、通常遷移先シーン名を渡して呼び出す）
    /// </summary>
    /// <param name="straightScene">「まっすぐ」を選んだ時の通常遷移先シーン名</param>
    public void ShowChoice(string straightScene)
    {
        straightSceneName = straightScene;

        awaitingChoice = true;
        rightRaisedTime = 0f;
        leftRaisedTime = 0f;

        if (branchUIRoot != null)
        {
            branchUIRoot.gameObject.SetActive(true);
            Debug.Log($"[RouteBranchChooser] 分岐UI表示 (まっすぐ先={straightSceneName})");
        }
        else
        {
            Debug.LogWarning("[RouteBranchChooser] branchUIRootが未設定です");
        }
    }

    /// <summary>
    /// 分岐UI表示中のみ、カメラ（MediaPipe Pose）の手挙げで選択を確定する
    /// 右手挙げ=まっすぐ / 左手挙げ=脇道（swapLeftRightで入替）。一定時間保持で確定（誤爆防止）
    /// </summary>
    private void Update()
    {
        if (!enableHandRaiseSelection || !awaitingChoice) return;
        if (branchUIRoot == null || !branchUIRoot.gameObject.activeInHierarchy) return;

        PoseLandmarkProvider provider = PoseLandmarkProvider.Instance;
        if (provider == null || !provider.HasValidData) return;

        bool rightRaised = IsHandRaised(provider, rightWristIndex, rightShoulderIndex);
        bool leftRaised = IsHandRaised(provider, leftWristIndex, leftShoulderIndex);

        rightRaisedTime = rightRaised ? rightRaisedTime + Time.deltaTime : 0f;
        leftRaisedTime = leftRaised ? leftRaisedTime + Time.deltaTime : 0f;

        if (rightRaisedTime >= holdTime)
        {
            awaitingChoice = false;
            Debug.Log("[RouteBranchChooser] 右手挙げ検知");
            if (!swapLeftRight) ChooseStraight(); else ChooseDetour();
        }
        else if (leftRaisedTime >= holdTime)
        {
            awaitingChoice = false;
            Debug.Log("[RouteBranchChooser] 左手挙げ検知");
            if (!swapLeftRight) ChooseDetour(); else ChooseStraight();
        }
    }

    /// <summary>
    /// 指定の手首が肩より上にあるか（MediaPipe正規化座標はy=0が上）
    /// </summary>
    private bool IsHandRaised(PoseLandmarkProvider provider, int wristIndex, int shoulderIndex)
    {
        if (provider.TryGetLandmark(wristIndex, out var wrist) &&
            provider.TryGetLandmark(shoulderIndex, out var shoulder))
        {
            return wrist.y < shoulder.y - raiseThreshold;
        }
        return false;
    }

    /// <summary>
    /// 「まっすぐ進む」選択（通常の遷移先へ）。UIボタンのOnClickに割り当て
    /// </summary>
    public void ChooseStraight()
    {
        Debug.Log($"[RouteBranchChooser] まっすぐ進む → {straightSceneName}");
        HideAndEnter(straightSceneName);
    }

    /// <summary>
    /// 「脇道」選択（新宿御苑シーンへ）。UIボタンのOnClickに割り当て
    /// </summary>
    public void ChooseDetour()
    {
        Debug.Log($"[RouteBranchChooser] 脇道 → {detourSceneName}");
        HideAndEnter(detourSceneName);
    }

    private void HideAndEnter(string sceneName)
    {
        awaitingChoice = false; // マウス選択時も手挙げ検知を停止
        if (branchUIRoot != null)
        {
            branchUIRoot.gameObject.SetActive(false);
        }
        ScenePortal portal = new ScenePortal();
        portal.Enter(sceneName);
    }
}
