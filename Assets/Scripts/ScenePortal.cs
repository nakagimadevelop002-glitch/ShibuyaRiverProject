using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// シーン間のポータル（入口/出口）
/// 各シーンに1つ配置し、Waypoint到達時にEnter()を呼び出すことでシーン遷移を実行
/// 円環状一方通行接続のため、各シーンには固定の開始位置が1つのみ存在
/// 責任: シーン遷移管理とスポーン地点管理（Fade演出はFadeManagerに委譲）
/// </summary>
public class ScenePortal : MonoBehaviour
{
    [Header("Start Position")]
    [Tooltip("このシーンの開始位置（Playerのスポーン地点）")]
    [SerializeField] private Transform startPosition;

    [Header("References")]
    [Tooltip("スポーン対象のPlayer（自動検索可能）")]
    [SerializeField] private Transform player;

    // シーン遷移実行フラグ（シーン間で保持）
    private static bool s_shouldSpawn = false;

    private void Start()
    {
        CheckAndSpawnPlayer();
    }

    /// <summary>
    /// ポータルに入る（シーン遷移実行）
    /// WaypointEventのonSceneTransitionから呼び出される
    /// </summary>
    /// <param name="targetSceneName">遷移先シーン名</param>
    public void Enter(string targetSceneName)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[ScenePortal] Target scene name is empty!");
            return;
        }

        Debug.Log($"[ScenePortal] Entering portal: Target={targetSceneName}");

        // スポーン実行フラグを立てる
        s_shouldSpawn = true;

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
    /// 意図: シーン遷移後、固定の開始位置にPlayerを配置
    /// </summary>
    private void CheckAndSpawnPlayer()
    {
        // スポーン実行フラグが立っていない場合はスキップ（初回起動等）
        if (!s_shouldSpawn)
        {
            Debug.Log("[ScenePortal] No spawn flag. Skipping spawn.");
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
                s_shouldSpawn = false; // クリア
                return;
            }
        }

        // 開始位置チェック
        if (startPosition == null)
        {
            Debug.LogWarning("[ScenePortal] Start position is not set!");
            s_shouldSpawn = false; // クリア
            return;
        }

        // Playerを開始位置に配置
        player.position = startPosition.position;
        player.rotation = startPosition.rotation;

        Debug.Log($"[ScenePortal] Player spawned at start position: {startPosition.name}");

        // 使用済みフラグをクリア
        s_shouldSpawn = false;
    }

    /// <summary>
    /// エディタテスト用にstatic変数をリセット
    /// 意図: PlayMode終了時やシーン単体テスト時の初期化
    /// </summary>
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_shouldSpawn = false;
        Debug.Log("[ScenePortal] Static variables reset for editor.");
    }
#endif
}
