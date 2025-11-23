using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[McpServerToolType, Description("Copy font settings from one Text component to another")]
public class FontCopyMCPTool
{
    [McpServerTool, Description("Copy font from source Text to target Text")]
    public async ValueTask<string> CopyFont(
        [Description("Source GameObject name")] string sourceObjectName,
        [Description("Target GameObject name")] string targetObjectName)
    {
        await UniTask.SwitchToMainThread();

        // Find source GameObject
        GameObject sourceObj = GameObject.Find(sourceObjectName);
        if (sourceObj == null)
        {
            return $"ERROR: Source GameObject '{sourceObjectName}' not found";
        }

        // Find target GameObject
        GameObject targetObj = GameObject.Find(targetObjectName);
        if (targetObj == null)
        {
            return $"ERROR: Target GameObject '{targetObjectName}' not found";
        }

        // Get Text components
        Text sourceText = sourceObj.GetComponent<Text>();
        if (sourceText == null)
        {
            return $"ERROR: Source GameObject '{sourceObjectName}' has no Text component";
        }

        Text targetText = targetObj.GetComponent<Text>();
        if (targetText == null)
        {
            return $"ERROR: Target GameObject '{targetObjectName}' has no Text component";
        }

        // Copy font
        Font sourceFont = sourceText.font;
        targetText.font = sourceFont;

        // Save scene
#if UNITY_EDITOR
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
#endif

        return $"SUCCESS: Copied font from '{sourceObjectName}' to '{targetObjectName}' (Font: {sourceFont.name})";
    }

    [McpServerTool, Description("Copy font from source to multiple targets")]
    public async ValueTask<string> CopyFontToMultiple(
        [Description("Source GameObject name")] string sourceObjectName,
        [Description("Target GameObject names (comma-separated)")] string targetObjectNames)
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
        string[] targets = targetObjectNames.Split(',');
        int successCount = 0;
        string errors = "";

        foreach (string targetName in targets)
        {
            string trimmedName = targetName.Trim();
            GameObject targetObj = GameObject.Find(trimmedName);

            if (targetObj == null)
            {
                errors += $"\n  - '{trimmedName}' not found";
                continue;
            }

            Text targetText = targetObj.GetComponent<Text>();
            if (targetText == null)
            {
                errors += $"\n  - '{trimmedName}' has no Text component";
                continue;
            }

            targetText.font = sourceFont;
            successCount++;
        }

        // Save scene
#if UNITY_EDITOR
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
#endif

        string result = $"SUCCESS: Copied font '{sourceFont.name}' to {successCount}/{targets.Length} targets";
        if (!string.IsNullOrEmpty(errors))
        {
            result += $"\nErrors:{errors}";
        }

        return result;
    }

    [McpServerTool, Description("Get font name from Text component")]
    public async ValueTask<string> GetFontName(
        [Description("GameObject name")] string objectName)
    {
        await UniTask.SwitchToMainThread();

        GameObject obj = GameObject.Find(objectName);
        if (obj == null)
        {
            return $"ERROR: GameObject '{objectName}' not found";
        }

        Text text = obj.GetComponent<Text>();
        if (text == null)
        {
            return $"ERROR: GameObject '{objectName}' has no Text component";
        }

        if (text.font == null)
        {
            return $"Font: NULL (using default)";
        }

        return $"Font: {text.font.name}";
    }

    [McpServerTool, Description("Copy font from source Text to all child Text components of target")]
    public async ValueTask<string> CopyFontToChildren(
        [Description("Source GameObject name")] string sourceObjectName,
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
            Debug.Log($"[FontCopy] Set font '{sourceFont.name}' for '{childText.gameObject.name}'");
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
