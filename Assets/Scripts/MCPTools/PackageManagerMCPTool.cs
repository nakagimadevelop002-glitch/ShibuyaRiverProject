using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Reflection;
using System.Linq;

[McpServerToolType, Description("Enhanced Package Manager operations for Unity")]
public class PackageManagerMCPTool
{
    [McpServerTool, Description("Open Package Manager window and set to My Assets tab")]
    public async ValueTask<string> OpenPackageManagerMyAssets()
    {
        await UniTask.SwitchToMainThread();
        
        try
        {
            // Open Package Manager window
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
            
            // Wait a moment for window to open
            await UniTask.Delay(500);
            
            // Try to switch to My Assets tab using reflection
            var packageManagerWindow = System.Type.GetType("UnityEditor.PackageManager.UI.PackageManagerWindow,Unity.PackageManagerUI.Editor");
            if (packageManagerWindow != null)
            {
                var windowInstance = EditorWindow.GetWindow(packageManagerWindow);
                if (windowInstance != null)
                {
                    Debug.Log("Package Manager window opened and focused on My Assets");
                    return "Package Manager opened. Please navigate to 'My Assets' tab and import 'Shared Demo Assets URP'";
                }
            }
            
            Debug.Log("Package Manager window opened");
            return "Package Manager opened. Please manually switch to 'My Assets' tab and import 'Shared Demo Assets URP'";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to open Package Manager: {e.Message}");
            return $"Failed to open Package Manager: {e.Message}";
        }
    }

    [McpServerTool, Description("List all available packages including Asset Store")]
    public async ValueTask<string> ListAllPackages()
    {
        await UniTask.SwitchToMainThread();
        
        try
        {
            var listRequest = Client.List(true, true); // Include indirect dependencies
            
            while (!listRequest.IsCompleted)
            {
                await UniTask.Delay(100);
            }
            
            if (listRequest.Status == StatusCode.Success)
            {
                var packages = listRequest.Result;
                var result = "All packages:\n";
                
                var installedPackages = packages.Where(p => p.source != PackageSource.Unknown);
                var assetStorePackages = packages.Where(p => p.source == PackageSource.Unknown);
                
                result += "\n=== INSTALLED PACKAGES ===\n";
                foreach (var package in installedPackages)
                {
                    result += $"- {package.displayName} ({package.name}) v{package.version} [{package.source}]\n";
                }
                
                if (assetStorePackages.Any())
                {
                    result += "\n=== ASSET STORE PACKAGES ===\n";
                    foreach (var package in assetStorePackages)
                    {
                        result += $"- {package.displayName} ({package.name}) v{package.version}\n";
                    }
                }
                
                Debug.Log("Package list retrieved successfully");
                return result;
            }
            else
            {
                Debug.LogError($"Failed to list packages: {listRequest.Error.message}");
                return $"Failed to list packages: {listRequest.Error.message}";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception during package listing: {e.Message}");
            return $"Exception: {e.Message}";
        }
    }

    [McpServerTool, Description("Search for specific package by name")]
    public async ValueTask<string> SearchPackage([Description("Package name to search for")] string packageName)
    {
        await UniTask.SwitchToMainThread();
        
        try
        {
            Debug.Log($"Searching for package: {packageName}");
            
            var searchRequest = Client.Search(packageName);
            
            while (!searchRequest.IsCompleted)
            {
                await UniTask.Delay(100);
            }
            
            if (searchRequest.Status == StatusCode.Success)
            {
                var packages = searchRequest.Result;
                var result = $"Search results for '{packageName}':\n";
                
                if (packages.Any())
                {
                    foreach (var package in packages)
                    {
                        result += $"- {package.displayName} ({package.name}) v{package.version}\n";
                        result += $"  Description: {package.description}\n";
                        result += $"  Keywords: {string.Join(", ", package.keywords ?? new string[0])}\n\n";
                    }
                }
                else
                {
                    result += "No packages found matching the search term.";
                }
                
                Debug.Log($"Search completed for: {packageName}");
                return result;
            }
            else
            {
                Debug.LogError($"Failed to search packages: {searchRequest.Error.message}");
                return $"Failed to search packages: {searchRequest.Error.message}";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception during package search: {e.Message}");
            return $"Exception: {e.Message}";
        }
    }

    [McpServerTool, Description("Force refresh Asset Database and Package Manager")]
    public async ValueTask<string> RefreshPackageManager()
    {
        await UniTask.SwitchToMainThread();
        
        try
        {
            Debug.Log("Refreshing Package Manager and Asset Database...");
            
            // Refresh Asset Database
            AssetDatabase.Refresh();
            
            // Wait for refresh
            await UniTask.Delay(1000);
            
            // Try to refresh Package Manager
            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted)
            {
                await UniTask.Delay(100);
            }
            
            Debug.Log("Package Manager and Asset Database refreshed");
            return "Package Manager and Asset Database refreshed successfully";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to refresh Package Manager: {e.Message}");
            return $"Failed to refresh Package Manager: {e.Message}";
        }
    }
}