using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEditor;

[McpServerToolType, Description("Manage EditorBuildSettings scenes")]
public class BuildSettingsMCPTool
{
    [McpServerTool, Description("Add a scene to EditorBuildSettings (no-op if already present, enabled by default)")]
    public async ValueTask<string> AddSceneToBuildSettings(
        [Description("Scene asset path, e.g. Assets/Scenes/TestScenes/Foo.unity")] string scenePath)
    {
        await UniTask.SwitchToMainThread();

        if (string.IsNullOrEmpty(scenePath))
        {
            return "ERROR: scenePath is required";
        }

        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == scenePath))
        {
            return $"Already in build settings: {scenePath}";
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        return $"SUCCESS: Added to build settings: {scenePath}";
    }

    [McpServerTool, Description("List scenes registered in EditorBuildSettings")]
    public async ValueTask<string> ListBuildSettingsScenes()
    {
        await UniTask.SwitchToMainThread();

        StringBuilder sb = new StringBuilder();
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length == 0)
        {
            return "No scenes in build settings";
        }
        for (int i = 0; i < scenes.Length; i++)
        {
            sb.AppendLine($"[{i}] {(scenes[i].enabled ? "enabled" : "disabled")} {scenes[i].path}");
        }
        return sb.ToString();
    }
}
