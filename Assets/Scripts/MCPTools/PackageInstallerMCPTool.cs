using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

[McpServerToolType, Description("Package installer for Unity Registry and Asset Store packages")]
public class PackageInstallerMCPTool
{
    [McpServerTool, Description("Install Cinemachine package from Unity Registry")]
    public async ValueTask<string> InstallCinemachine()
    {
        await UniTask.SwitchToMainThread();
        
        try
        {
            Debug.Log("Starting Cinemachine installation...");
            
            // Install Cinemachine package
            var addRequest = Client.Add("com.unity.cinemachine");
            
            while (!addRequest.IsCompleted)
            {
                await UniTask.Delay(100);
            }
            
            if (addRequest.Status == StatusCode.Success)
            {
                Debug.Log("Cinemachine installed successfully!");
                return "SUCCESS: Cinemachine package installed successfully! You can now use virtual cameras and advanced camera controls.";
            }
            else
            {
                Debug.LogError($"Failed to install Cinemachine: {addRequest.Error.message}");
                return $"FAILED: Could not install Cinemachine - {addRequest.Error.message}";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception during Cinemachine installation: {e.Message}");
            return $"EXCEPTION: {e.Message}";
        }
    }

    [McpServerTool, Description("Install TextMeshPro package from Unity Registry")]
    public async ValueTask<string> InstallTextMeshPro()
    {
        await UniTask.SwitchToMainThread();
        
        try
        {
            Debug.Log("Starting TextMeshPro installation...");
            
            var addRequest = Client.Add("com.unity.textmeshpro");
            
            while (!addRequest.IsCompleted)
            {
                await UniTask.Delay(100);
            }
            
            if (addRequest.Status == StatusCode.Success)
            {
                Debug.Log("TextMeshPro installed successfully!");
                return "SUCCESS: TextMeshPro package installed successfully! Enhanced text rendering is now available.";
            }
            else
            {
                Debug.LogError($"Failed to install TextMeshPro: {addRequest.Error.message}");
                return $"FAILED: Could not install TextMeshPro - {addRequest.Error.message}";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception during TextMeshPro installation: {e.Message}");
            return $"EXCEPTION: {e.Message}";
        }
    }

    [McpServerTool, Description("Install any Unity Registry package by name")]
    public async ValueTask<string> InstallPackage([Description("Package name (e.g., com.unity.cinemachine)")] string packageName)
    {
        await UniTask.SwitchToMainThread();
        
        try
        {
            Debug.Log($"Starting installation of package: {packageName}");
            
            var addRequest = Client.Add(packageName);
            
            while (!addRequest.IsCompleted)
            {
                await UniTask.Delay(100);
            }
            
            if (addRequest.Status == StatusCode.Success)
            {
                Debug.Log($"Package {packageName} installed successfully!");
                return $"SUCCESS: Package '{packageName}' installed successfully!";
            }
            else
            {
                Debug.LogError($"Failed to install {packageName}: {addRequest.Error.message}");
                return $"FAILED: Could not install '{packageName}' - {addRequest.Error.message}";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception during package installation: {e.Message}");
            return $"EXCEPTION: {e.Message}";
        }
    }

    [McpServerTool, Description("Import Fur Hair And Fiber Shader from Asset Store")]
    public async ValueTask<string> ImportFurHairFiberShader()
    {
        await UniTask.SwitchToMainThread();
        
        try
        {
            Debug.Log("Starting Fur Hair And Fiber Shader import from Asset Store...");
            
            // Try to import using AssetDatabase.ImportPackage
            string packagePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "Unity/Asset Store-5.x/Neko Legends/Shaders/Fur Hair And Fiber Shader.unitypackage"
            );
            
            if (System.IO.File.Exists(packagePath))
            {
                AssetDatabase.ImportPackage(packagePath, false); // false = import without dialog
                Debug.Log("Fur Hair And Fiber Shader imported successfully!");
                return "SUCCESS: Fur Hair And Fiber Shader imported successfully from Asset Store!";
            }
            else
            {
                Debug.LogWarning($"Package file not found at: {packagePath}");
                return $"PACKAGE NOT FOUND: Please download the package from Asset Store first. Expected location: {packagePath}";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception during Asset Store import: {e.Message}");
            return $"EXCEPTION: {e.Message}";
        }
    }
    
    [McpServerTool, Description("Import any Asset Store package by name")]
    public async ValueTask<string> ImportAssetStorePackage([Description("Asset Store package name")] string packageName)
    {
        await UniTask.SwitchToMainThread();
        
        try
        {
            Debug.Log($"Starting Asset Store package import: {packageName}");
            
            // Search common Asset Store download locations
            string[] searchPaths = {
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Unity/Asset Store-5.x"),
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Unity/Asset Store")
            };
            
            foreach (string basePath in searchPaths)
            {
                if (System.IO.Directory.Exists(basePath))
                {
                    var packageFiles = System.IO.Directory.GetFiles(basePath, "*.unitypackage", System.IO.SearchOption.AllDirectories);
                    var matchingPackage = System.Array.Find(packageFiles, p => p.Contains(packageName));
                    
                    if (!string.IsNullOrEmpty(matchingPackage))
                    {
                        AssetDatabase.ImportPackage(matchingPackage, false);
                        Debug.Log($"Asset Store package imported: {packageName}");
                        return $"SUCCESS: Asset Store package '{packageName}' imported successfully!";
                    }
                }
            }
            
            return $"PACKAGE NOT FOUND: Could not locate '{packageName}' in Asset Store downloads. Please download from Asset Store first.";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception during Asset Store import: {e.Message}");
            return $"EXCEPTION: {e.Message}";
        }
    }
}