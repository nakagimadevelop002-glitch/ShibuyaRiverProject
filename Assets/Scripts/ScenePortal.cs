using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン間のポータル（入口/出口）
/// Waypoint到達時にEnter()を呼び出すことでシーン遷移を実行
/// 責任: フェード完了後のシーン遷移実行（フェード演出はFadeManagerに委譲）
/// MonoBehaviourを継承しない純粋なC#クラス
/// </summary>
public class ScenePortal
{
    /// <summary>
    /// ポータルに入る（シーン遷移実行）
    /// RouteFollowerから動的インスタンス生成して呼び出される
    /// </summary>
    /// <param name="targetSceneName">遷移先シーン名</param>
    public void Enter(string targetSceneName)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[ScenePortal] Target scene name is empty!");
            return;
        }

        // Debug.Log($"[ScenePortal] Entering portal: Target={targetSceneName}");

        // FadeManager検索
        FadeManager fadeManager = Object.FindFirstObjectByType<FadeManager>();

        if (fadeManager != null)
        {
            // フェード演出後にシーン遷移（コールバック方式）
            fadeManager.FadeOutWithCallback(() =>
            {
                // フェード完了後にScenePortalがシーン遷移を実行
                SceneManager.LoadScene(targetSceneName);
            });
        }
        else
        {
            // FadeManagerが見つからない場合は直接シーン遷移
            Debug.LogWarning("[ScenePortal] FadeManager not found. Loading scene without fade.");
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
