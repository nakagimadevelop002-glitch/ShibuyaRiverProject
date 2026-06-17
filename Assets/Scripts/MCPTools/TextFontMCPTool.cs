using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

[McpServerToolType, Description("Assign a Font asset and font size to legacy UI Text components")]
public class TextFontMCPTool
{
    [McpServerTool, Description("Set the Font asset (and optional font size) on all Text components under a GameObject (including itself and children)")]
    public async ValueTask<string> SetTextFontFromAsset(
        [Description("Target GameObject name (Text components on it and its children are updated)")] string objectName,
        [Description("Font asset path, e.g. Assets/Fonts/BIZ-UDGothicR.ttc")] string fontAssetPath,
        [Description("Font size to apply (0 = keep current)")] int fontSize = 0)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject target = GameObject.Find(objectName);
            if (target == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(fontAssetPath);
            if (font == null)
            {
                return $"ERROR: Font asset not found at '{fontAssetPath}'";
            }

            Text[] texts = target.GetComponentsInChildren<Text>(true);
            if (texts.Length == 0)
            {
                return $"ERROR: No Text components found under '{objectName}'";
            }

            foreach (Text t in texts)
            {
                t.font = font;
                if (fontSize > 0)
                {
                    t.fontSize = fontSize;
                }
                EditorUtility.SetDirty(t);
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            return $"SUCCESS: Set font '{font.name}'{(fontSize > 0 ? $" size {fontSize}" : "")} on {texts.Length} Text component(s) under '{objectName}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set text font: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }
}
