using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("Create and manage ScriptableObject assets")]
public class ScriptableObjectMCPTool
{
    [McpServerTool, Description("Create a ScriptableObject asset by type name")]
    public async ValueTask<string> CreateScriptableObject(
        [Description("ScriptableObject type name (e.g., CardDatabase)")] string typeName,
        [Description("Asset file path relative to Assets folder (e.g., _Project/Data/CardDatabase.asset)")] string assetPath)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();

            // Find the ScriptableObject type
            var scriptableObjectType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == typeName && typeof(ScriptableObject).IsAssignableFrom(t));

            if (scriptableObjectType == null)
            {
                return $"ERROR: ScriptableObject type '{typeName}' not found";
            }

            // Create the ScriptableObject instance
            var instance = ScriptableObject.CreateInstance(scriptableObjectType);
            if (instance == null)
            {
                return $"ERROR: Failed to create instance of '{typeName}'";
            }

            // Ensure path starts with "Assets/"
            if (!assetPath.StartsWith("Assets/"))
            {
                assetPath = "Assets/" + assetPath;
            }

            // Ensure .asset extension
            if (!assetPath.EndsWith(".asset"))
            {
                assetPath += ".asset";
            }

            // Create directory if it doesn't exist
            string directory = System.IO.Path.GetDirectoryName(assetPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Create the asset using reflection to avoid compile errors in runtime
            var assetDatabaseType = typeof(UnityEditor.AssetDatabase);
            var createAssetMethod = assetDatabaseType.GetMethod("CreateAsset", new[] { typeof(UnityEngine.Object), typeof(string) });
            var saveAssetsMethod = assetDatabaseType.GetMethod("SaveAssets", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var refreshMethod = assetDatabaseType.GetMethod("Refresh", new Type[] { });

            createAssetMethod.Invoke(null, new object[] { instance, assetPath });
            saveAssetsMethod.Invoke(null, null);
            refreshMethod.Invoke(null, null);

            Debug.Log($"ScriptableObject '{typeName}' created at {assetPath}");
            return $"SUCCESS: Created '{typeName}' at {assetPath}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create ScriptableObject: {e.Message}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: ScriptableObject creation only available in Unity Editor";
#endif
    }

    [McpServerTool, Description("List all ScriptableObject assets of a specific type")]
    public async ValueTask<string> ListScriptableObjects(
        [Description("ScriptableObject type name (e.g., CardDatabase)")] string typeName)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();

            // Find the ScriptableObject type
            var scriptableObjectType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == typeName && typeof(ScriptableObject).IsAssignableFrom(t));

            if (scriptableObjectType == null)
            {
                return $"ERROR: ScriptableObject type '{typeName}' not found";
            }

            // Find all assets using reflection
            var assetDatabaseType = typeof(UnityEditor.AssetDatabase);
            var findAssetsMethod = assetDatabaseType.GetMethod("FindAssets", new[] { typeof(string) });
            var guidToAssetPathMethod = assetDatabaseType.GetMethod("GUIDToAssetPath", new[] { typeof(string) });
            var loadAssetAtPathMethod = assetDatabaseType.GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(Type) });

            string[] guids = (string[])findAssetsMethod.Invoke(null, new object[] { $"t:{typeName}" });

            if (guids.Length == 0)
            {
                return $"No {typeName} assets found in project";
            }

            string result = $"Found {guids.Length} {typeName} asset(s):\n";
            foreach (string guid in guids)
            {
                string path = (string)guidToAssetPathMethod.Invoke(null, new object[] { guid });
                var asset = loadAssetAtPathMethod.Invoke(null, new object[] { path, scriptableObjectType });
                if (asset != null)
                {
                    result += $"- {((UnityEngine.Object)asset).name} at {path}\n";
                }
            }

            return result.Trim();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to list ScriptableObjects: {e.Message}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: ScriptableObject listing only available in Unity Editor";
#endif
    }

    [McpServerTool, Description("Load a ScriptableObject asset by path and return as string reference")]
    public async ValueTask<string> LoadScriptableObject(
        [Description("Asset path relative to Assets folder or asset name")] string assetPath)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();

            // Ensure path starts with "Assets/" if it's a path
            if (assetPath.Contains("/") && !assetPath.StartsWith("Assets/"))
            {
                assetPath = "Assets/" + assetPath;
            }

            // Use reflection to access AssetDatabase
            var assetDatabaseType = typeof(UnityEditor.AssetDatabase);
            var loadAssetAtPathMethod = assetDatabaseType.GetMethod("LoadAssetAtPath", 1,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null, new[] { typeof(string) }, null);
            var findAssetsMethod = assetDatabaseType.GetMethod("FindAssets", new[] { typeof(string) });
            var guidToAssetPathMethod = assetDatabaseType.GetMethod("GUIDToAssetPath", new[] { typeof(string) });

            // Try generic method
            var genericMethod = assetDatabaseType.GetMethod("LoadAssetAtPath").MakeGenericMethod(typeof(ScriptableObject));
            var asset = (ScriptableObject)genericMethod.Invoke(null, new object[] { assetPath });

            // If not found, try to find by name
            if (asset == null)
            {
                string[] guids = (string[])findAssetsMethod.Invoke(null, new object[] { assetPath });
                if (guids.Length > 0)
                {
                    string path = (string)guidToAssetPathMethod.Invoke(null, new object[] { guids[0] });
                    asset = (ScriptableObject)genericMethod.Invoke(null, new object[] { path });
                    assetPath = path;
                }
            }

            if (asset == null)
            {
                return $"ERROR: ScriptableObject not found at '{assetPath}'";
            }

            return $"SUCCESS: Loaded '{asset.name}' ({asset.GetType().Name}) from {assetPath}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load ScriptableObject: {e.Message}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: ScriptableObject loading only available in Unity Editor";
#endif
    }
}
