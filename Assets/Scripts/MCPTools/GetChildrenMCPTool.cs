using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using System.Text;

[McpServerToolType, Description("Get GameObject children hierarchy")]
public class GetChildrenMCPTool
{
    [McpServerTool, Description("Get all children of a GameObject")]
    public async ValueTask<string> GetChildren(
        [Description("Parent GameObject name")] string parentName)
    {
        await UniTask.SwitchToMainThread();

        GameObject parent = GameObject.Find(parentName);
        if (parent == null)
        {
            return $"ERROR: GameObject '{parentName}' not found";
        }

        Transform parentTransform = parent.transform;
        int childCount = parentTransform.childCount;

        if (childCount == 0)
        {
            return $"'{parentName}' has no children";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"'{parentName}' children ({childCount} total):");

        for (int i = 0; i < childCount; i++)
        {
            Transform child = parentTransform.GetChild(i);
            sb.AppendLine($"  [{i}] {child.name}");
        }

        return sb.ToString();
    }
}
