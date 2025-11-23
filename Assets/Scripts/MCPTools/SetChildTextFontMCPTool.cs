using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[McpServerToolType, Description("Set font for child Text components")]
public class SetChildTextFontMCPTool
{
    [McpServerTool, Description("Copy font from source Text to all Text components in target's children")]
    public async ValueTask<string> SetChildTextFont(
        [Description("Source GameObject name (with Text component)")] string sourceObjectName,
        [Description("Target parent GameObject name")] string targetParentName)
    {
        await UniTask.SwitchToMainThread();

        // Find source GameObject
        GameObject sourceObj = GameObject.Find(sourceObjectName);
        if (sourceObj == null)
        {
            return $"ERROR: Source GameObject '{sourceObjectName}' not found";
        }

        // Get source Text component
        Text sourceText = sourceObj.GetComponent<Text>();
        if (sourceText == null)
        {
            return $"ERROR: Source GameObject '{sourceObjectName}' has no Text component";
        }

        Font sourceFont = sourceText.font;
        if (sourceFont == null)
        {
            return $"ERROR: Source Text has null font";
        }

        // Find target parent GameObject
        GameObject targetParent = GameObject.Find(targetParentName);
        if (targetParent == null)
        {
            return $"ERROR: Target parent GameObject '{targetParentName}' not found";
        }

        // Get all Text components in children (including inactive)
        Text[] childTexts = targetParent.GetComponentsInChildren<Text>(true);

        if (childTexts.Length == 0)
        {
            return $"WARNING: No Text components found in '{targetParentName}' children";
        }

        int count = 0;
        foreach (Text childText in childTexts)
        {
            childText.font = sourceFont;
            Debug.Log($"[SetChildTextFont] Set font '{sourceFont.name}' for '{childText.gameObject.name}'");
            count++;
        }

        // Save scene
#if UNITY_EDITOR
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Scene '{scene.name}' saved after setting fonts");
#endif

        return $"SUCCESS: Set font '{sourceFont.name}' for {count} Text component(s) in '{targetParentName}' children";
    }
}
