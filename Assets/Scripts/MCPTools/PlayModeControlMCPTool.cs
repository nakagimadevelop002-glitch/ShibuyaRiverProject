using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEditor;

[McpServerToolType, Description("Control Unity Play Mode")]
public class PlayModeControlMCPTool
{
    [McpServerTool, Description("Enter Play Mode")]
    public async ValueTask<string> EnterPlayMode()
    {
        await UniTask.SwitchToMainThread();

        if (EditorApplication.isPlaying)
        {
            return "INFO: Already in Play Mode";
        }

        EditorApplication.isPlaying = true;
        Debug.Log("PlayModeControl: Entering Play Mode");

        return "SUCCESS: Entering Play Mode";
    }

    [McpServerTool, Description("Exit Play Mode")]
    public async ValueTask<string> ExitPlayMode()
    {
        await UniTask.SwitchToMainThread();

        if (!EditorApplication.isPlaying)
        {
            return "INFO: Already in Edit Mode";
        }

        EditorApplication.isPlaying = false;
        Debug.Log("PlayModeControl: Exiting Play Mode");

        return "SUCCESS: Exiting Play Mode";
    }

    [McpServerTool, Description("Check if currently in Play Mode")]
    public async ValueTask<string> IsPlayMode()
    {
        await UniTask.SwitchToMainThread();

        bool isPlaying = EditorApplication.isPlaying;
        return $"Play Mode: {isPlaying}";
    }
}
