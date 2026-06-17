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
    [SerializeField] private string detourSceneName = "ShinjukuJGixyoenRiverMapTest";

    // まっすぐ進む場合の遷移先（RouteFollowerが最終WayPointのtargetSceneNameを渡す）
    private string straightSceneName;

    /// <summary>
    /// 分岐UIを表示する（RouteFollowerが最終地点到達時に、通常遷移先シーン名を渡して呼び出す）
    /// </summary>
    /// <param name="straightScene">「まっすぐ」を選んだ時の通常遷移先シーン名</param>
    public void ShowChoice(string straightScene)
    {
        straightSceneName = straightScene;

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
        if (branchUIRoot != null)
        {
            branchUIRoot.gameObject.SetActive(false);
        }
        ScenePortal portal = new ScenePortal();
        portal.Enter(sceneName);
    }
}
