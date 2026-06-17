using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEditor;

[McpServerToolType, Description("Manage EditorBuildSettings scenes and move/rename assets")]
public class BuildSettingsMCPTool
{
    [McpServerTool, Description("Move or rename an asset (AssetDatabase.MoveAsset; preserves GUID and updates references). Destination folder must exist.")]
    public async ValueTask<string> MoveAsset(
        [Description("Source asset path, e.g. Assets/Scenes/TestScenes/Foo.unity")] string fromPath,
        [Description("Destination asset path, e.g. Assets/Scenes/Bar.unity")] string toPath)
    {
        await UniTask.SwitchToMainThread();

        if (string.IsNullOrEmpty(fromPath) || string.IsNullOrEmpty(toPath))
        {
            return "ERROR: fromPath and toPath are required";
        }

        string error = AssetDatabase.MoveAsset(fromPath, toPath);
        if (!string.IsNullOrEmpty(error))
        {
            return $"ERROR: {error}";
        }
        AssetDatabase.SaveAssets();
        return $"SUCCESS: Moved {fromPath} -> {toPath}";
    }

    [McpServerTool, Description("Remove a scene from EditorBuildSettings by path")]
    public async ValueTask<string> RemoveSceneFromBuildSettings(
        [Description("Scene asset path to remove")] string scenePath)
    {
        await UniTask.SwitchToMainThread();

        var scenes = EditorBuildSettings.scenes.ToList();
        int removed = scenes.RemoveAll(s => s.path == scenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
        return removed > 0 ? $"SUCCESS: Removed {removed} entry: {scenePath}" : $"Not found in build settings: {scenePath}";
    }
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
