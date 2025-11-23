using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// シーン間のポータル（入口/出口）
/// 各シーンに1つ配置し、Waypoint到達時にEnter()を呼び出すことでシーン遷移を実行
/// 責任: シーン遷移管理とスポーン地点管理（Fade演出はFadeManagerに委譲）
/// </summary>
public class ScenePortal : MonoBehaviour
{
    /// <summary>
    /// スポーン地点情報
    /// </summary>
    [System.Serializable]
    public class SpawnPoint
    {
        [Tooltip("ポータルID（遷移元から指定される識別子）")]
        public string portalID;

        [Tooltip("スポーン位置・回転")]
        public Transform position;
    }

    [Header("Spawn Points")]
    [Tooltip("このシーンのスポーン地点一覧")]
    [SerializeField] private SpawnPoint[] spawnPoints;

    [Header("References")]
    [Tooltip("スポーン対象のPlayer（自動検索可能）")]
    [SerializeField] private Transform player;

    // シーン間で受け渡すポータルID
    private static string s_nextPortalID = "";

    private void Start()
    {
        CheckAndSpawnPlayer();
    }

    /// <summary>
    /// ポータルに入る（シーン遷移実行）
    /// WaypointEventのonReachedから呼び出される
    /// </summary>
    /// <param name="targetSceneName">遷移先シーン名</param>
    /// <param name="destinationPortalID">遷移先でのスポーン地点ID</param>
    public void Enter(string targetSceneName, string destinationPortalID)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[ScenePortal] Target scene name is empty!");
            return;
        }

        Debug.Log($"[ScenePortal] Entering portal: Target={targetSceneName}, SpawnID={destinationPortalID}");

        // 次シーンのスポーン地点IDを保存
        s_nextPortalID = destinationPortalID;

        // Fade + シーン遷移実行
        StartCoroutine(TransitionCoroutine(targetSceneName));
    }

    /// <summary>
    /// シーン遷移のコルーチン実装
    /// 意図: FadeOut → シーン読み込み → FadeIn の順序制御（Fade演出はFadeManagerに委譲）
    /// </summary>
    private IEnumerator TransitionCoroutine(string targetSceneName)
    {
        // FadeManager存在確認
        if (FadeManager.Instance == null)
        {
            Debug.LogError("[ScenePortal] FadeManager.Instance is null! Please add FadeManager to the scene.");
            yield break;
        }

        // 1. FadeOut（FadeManagerに委譲）
        yield return StartCoroutine(FadeManager.Instance.FadeOut());

        // 2. シーン読み込み
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 3. FadeIn（FadeManagerに委譲）
        yield return StartCoroutine(FadeManager.Instance.FadeIn());
    }

    /// <summary>
    /// シーン開始時のPlayerスポーン処理
    /// 意図: 保存されたポータルIDに対応する位置にPlayerを配置
    /// </summary>
    private void CheckAndSpawnPlayer()
    {
        // ポータルID指定がない場合はスキップ（初回起動等）
        if (string.IsNullOrEmpty(s_nextPortalID))
        {
            Debug.Log("[ScenePortal] No portal ID specified. Skipping spawn.");
            return;
        }

        // Player自動検索（Inspector未設定時）
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("[ScenePortal] Player not found! Please assign or tag Player object.");
                s_nextPortalID = ""; // クリア
                return;
            }
        }

        // スポーン地点を検索
        SpawnPoint spawnPoint = System.Array.Find(spawnPoints, sp => sp.portalID == s_nextPortalID);

        if (spawnPoint != null && spawnPoint.position != null)
        {
            // Playerを配置
            player.position = spawnPoint.position.position;
            player.rotation = spawnPoint.position.rotation;

            Debug.Log($"[ScenePortal] Player spawned at: {s_nextPortalID}");
        }
        else
        {
            Debug.LogWarning($"[ScenePortal] Spawn point not found: {s_nextPortalID}");
        }

        // 使用済みフラグをクリア
        s_nextPortalID = "";
    }

    /// <summary>
    /// エディタテスト用にstatic変数をリセット
    /// 意図: PlayMode終了時やシーン単体テスト時の初期化
    /// </summary>
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_nextPortalID = "";
        Debug.Log("[ScenePortal] Static variables reset for editor.");
    }
#endif
}
