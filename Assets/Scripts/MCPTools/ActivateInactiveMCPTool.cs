using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

// 非アクティブなGameObjectも検索して有効/無効を切り替える復旧用ツール。
// 標準の SetGameObjectActive は GameObject.Find を使うため、一度無効化したオブジェクトを再有効化できない。
// このツールは scene.GetRootGameObjects()（非アクティブなルートも含む）を再帰探索して対象を見つける。
[McpServerToolType, Description("Set active state for GameObjects including inactive ones")]
public class ActivateInactiveMCPTool
{
    [McpServerTool, Description("Set GameObject active state, searching inactive objects too (recovery for objects disabled by SetGameObjectActive)")]
    public async ValueTask<string> SetActiveIncludeInactive(
        [Description("Target GameObject name")] string objectName,
        [Description("Active state (true = enabled, false = disabled)")] string active)
    {
#if UNITY_EDITOR
        await UniTask.SwitchToMainThread();

        var scene = SceneManager.GetActiveScene();
        GameObject target = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            target = FindByNameRecursive(root.transform, objectName);
            if (target != null) break;
        }

        if (target == null)
        {
            return $"ERROR: GameObject '{objectName}' not found (including inactive)";
        }

        bool activeState = active.ToLower() == "true" || active == "1";
        target.SetActive(activeState);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Scene '{scene.name}' saved after setting {objectName}.SetActive({activeState}) [include inactive]");

        return $"SUCCESS: Set {objectName}.SetActive({activeState})";
#else
        await UniTask.Yield();
        return "ERROR: only available in Unity Editor";
#endif
    }

    private static GameObject FindByNameRecursive(Transform t, string name)
    {
        if (t.name == name) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var found = FindByNameRecursive(t.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
