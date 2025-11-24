using UnityEngine;
using System.Collections;

/// <summary>
/// シーン間のポータル（入口/出口）
/// Waypoint到達時にEnter()を呼び出すことでシーン遷移を実行
/// 責任: シーン遷移トリガー（Fade演出はFadeManagerに委譲）
/// </summary>
public class ScenePortal : MonoBehaviour
{
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

        // Fade + シーン遷移実行
        StartCoroutine(TransitionCoroutine(targetSceneName));
    }

    /// <summary>
    /// シーン遷移のコルーチン実装
    /// 意図: FadeOut → シーン読み込み（Fade演出はFadeManagerに委譲）
    /// 次シーンのFadeManagerが自動的にFadeInを実行
    /// </summary>
    private IEnumerator TransitionCoroutine(string targetSceneName)
    {
        // FadeManager検索
        FadeManager fadeManager = FindObjectOfType<FadeManager>();
        if (fadeManager == null)
        {
            Debug.LogError("[ScenePortal] FadeManager not found! Please add FadeManager to the scene.");
            yield break;
        }

        // FadeManager.LoadSceneWithFade()を使用（FadeOut完了後にシーンロード）
        fadeManager.LoadSceneWithFade(targetSceneName);

        // Coroutine完了を待つ必要なし（FadeManagerが全て処理）
        yield break;
    }
}
