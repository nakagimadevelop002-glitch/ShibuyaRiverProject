using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("Manage GameObject hierarchy and sibling order")]
public class HierarchyManagerMCPTool
{
    [McpServerTool, Description("Set GameObject sibling index in Hierarchy (drawing order for UI)")]
    public async ValueTask<string> SetSiblingIndex(
        [Description("Target GameObject name")] string objectName,
        [Description("Sibling index (0=first/top, higher=later/bottom)")] int siblingIndex)
    {
        await UniTask.SwitchToMainThread();

        GameObject targetObject = GameObject.Find(objectName);
        if (targetObject == null)
        {
            return $"ERROR: GameObject '{objectName}' not found";
        }

        Transform transform = targetObject.transform;
        int maxIndex = transform.parent != null ? transform.parent.childCount - 1 : 0;

        if (siblingIndex < 0)
        {
            siblingIndex = 0;
        }

        transform.SetSiblingIndex(siblingIndex);
        int actualIndex = transform.GetSiblingIndex();

        Debug.Log($"Set sibling index for '{objectName}': requested {siblingIndex}, actual {actualIndex}");
        return $"SUCCESS: Set sibling index for '{objectName}' to {actualIndex} (max: {maxIndex})";
    }

    [McpServerTool, Description("Get GameObject sibling index")]
    public async ValueTask<string> GetSiblingIndex(
        [Description("Target GameObject name")] string objectName)
    {
        await UniTask.SwitchToMainThread();

        GameObject targetObject = GameObject.Find(objectName);
        if (targetObject == null)
        {
            return $"ERROR: GameObject '{objectName}' not found";
        }

        int siblingIndex = targetObject.transform.GetSiblingIndex();
        int totalSiblings = targetObject.transform.parent != null
            ? targetObject.transform.parent.childCount
            : 0;

        return $"SUCCESS: '{objectName}' sibling index: {siblingIndex} (total siblings: {totalSiblings})";
    }

    [McpServerTool, Description("List all GameObjects in the scene hierarchy")]
    public async ValueTask<string> ListGameObjects()
    {
        await UniTask.SwitchToMainThread();

        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        // ルートオブジェクトのみを抽出
        var rootObjects = new System.Collections.Generic.List<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.transform.parent == null)
            {
                rootObjects.Add(obj);
            }
        }

        var result = new System.Text.StringBuilder();
        result.AppendLine($"Scene Hierarchy ({allObjects.Length} total objects):");
        result.AppendLine("===================================");

        foreach (var root in rootObjects)
        {
            AppendHierarchy(root.transform, result, 0);
        }

        return result.ToString();
    }

    private void AppendHierarchy(Transform transform, System.Text.StringBuilder sb, int depth)
    {
        string indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}- {transform.name}");

        for (int i = 0; i < transform.childCount; i++)
        {
            AppendHierarchy(transform.GetChild(i), sb, depth + 1);
        }
    }
}
