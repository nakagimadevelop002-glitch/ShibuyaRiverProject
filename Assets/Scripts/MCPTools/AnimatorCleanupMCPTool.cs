using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

[McpServerToolType, Description("Clean up AnimatorController issues")]
public class AnimatorCleanupMCPTool
{
    [McpServerTool, Description("Remove AnyState transitions that have no conditions AND no exit time (these are ignored by Unity and produce 'transition will be ignored' warnings). API-based, safe.")]
    public async ValueTask<string> RemoveInvalidAnyStateTransitions(
        [Description("AnimatorController asset path, e.g. Assets/.../Foo.controller")] string controllerPath)
    {
        await UniTask.SwitchToMainThread();

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            return $"ERROR: AnimatorController not found at '{controllerPath}'";
        }

        int removed = 0;
        foreach (var layer in controller.layers)
        {
            if (layer.stateMachine != null)
            {
                removed += CleanStateMachine(layer.stateMachine);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return $"SUCCESS: Removed {removed} conditionless/no-exit-time AnyState transition(s) in '{controllerPath}'";
    }

    private int CleanStateMachine(AnimatorStateMachine sm)
    {
        int removed = 0;

        var toRemove = new List<AnimatorStateTransition>();
        foreach (var t in sm.anyStateTransitions)
        {
            bool noCondition = (t.conditions == null || t.conditions.Length == 0);
            if (noCondition && !t.hasExitTime)
            {
                toRemove.Add(t);
            }
        }
        foreach (var t in toRemove)
        {
            sm.RemoveAnyStateTransition(t);
            removed++;
        }

        // 子ステートマシンも再帰処理
        foreach (var child in sm.stateMachines)
        {
            if (child.stateMachine != null)
            {
                removed += CleanStateMachine(child.stateMachine);
            }
        }

        return removed;
    }
}
