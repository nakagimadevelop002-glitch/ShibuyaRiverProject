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
}
