using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEditor;
using System.IO;

[McpServerToolType, Description("Export assets as a UnityPackage including all dependencies")]
public class ExportPackageMCPTool
{
    [McpServerTool, Description("Export an asset (e.g. a prefab) and all of its dependencies (child models, materials, textures, shaders) as a .unitypackage")]
    public async ValueTask<string> ExportAssetPackageWithDependencies(
        [Description("Asset path inside the project, e.g. Assets/Prefab/ShinjukuGyoenn.prefab")] string assetPath,
        [Description("Output .unitypackage path. If empty, exported to <ProjectRoot>/<AssetName>.unitypackage")] string outputPath = "")
    {
        try
        {
            await UniTask.SwitchToMainThread();

            if (string.IsNullOrEmpty(assetPath))
            {
                return "assetPath is required";
            }

            // Verify the asset exists
            UnityEngine.Object mainAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (mainAsset == null)
            {
                return $"Asset not found at '{assetPath}'";
            }

            // Resolve full dependency tree (recurse) so nothing is missing
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);

            // Determine output path (outside Assets so it does not re-import into the project)
            if (string.IsNullOrEmpty(outputPath))
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                outputPath = Path.Combine(projectRoot, assetName + ".unitypackage");
            }

            // Export with dependencies, recursing into folders
            AssetDatabase.ExportPackage(
                assetPath,
                outputPath,
                ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse);

            string report = "Exported package to: " + outputPath + "\n";
            report += "Included asset dependencies (" + dependencies.Length + "):\n";
            foreach (string dep in dependencies.OrderBy(d => d))
            {
                report += "- " + dep + "\n";
            }

            Debug.Log("Exported UnityPackage: " + outputPath + " (" + dependencies.Length + " dependencies)");
            return report;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to export package: " + e.Message);
            throw;
        }
    }
}
