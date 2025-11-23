using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEditor;
using System.IO;

[McpServerToolType, Description("Manage Prefab creation and operations")]
public class PrefabManagerMCPTool
{
    [McpServerTool, Description("Create prefab from GameObject in scene")]
    public async ValueTask<string> CreatePrefabFromGameObject(
        [Description("GameObject name in scene")] string objectName,
        [Description("Prefab name (without .prefab extension)")] string prefabName = "")
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return $"GameObject '{objectName}' not found in scene";
            }

            if (string.IsNullOrEmpty(prefabName))
            {
                prefabName = objectName;
            }

            // Ensure Prefabs directory exists
            string prefabDir = "Assets/_Project/Prefabs";
            if (!Directory.Exists(prefabDir))
            {
                Directory.CreateDirectory(prefabDir);
                AssetDatabase.Refresh();
            }

            string prefabPath = $"{prefabDir}/{prefabName}.prefab";

            // Save mesh as asset if it has MeshFilter
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                string meshPath = $"{prefabDir}/{prefabName}_Mesh.asset";
                AssetDatabase.CreateAsset(meshFilter.sharedMesh, meshPath);
            }

            // Save material as asset if it has MeshRenderer
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            {
                string materialPath = $"{prefabDir}/{prefabName}_Material.mat";
                AssetDatabase.CreateAsset(new Material(meshRenderer.sharedMaterial), materialPath);
            }

            // Create prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);

            if (prefab != null)
            {
                Debug.Log($"Created prefab '{prefabName}' at {prefabPath}");
                return $"Successfully created prefab '{prefabName}' at {prefabPath}";
            }
            else
            {
                return $"Failed to create prefab '{prefabName}'";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create prefab: {e.Message}");
            throw;
        }
    }

    [McpServerTool, Description("Create prefabs from all GameObjects in scene (except Camera and Light)")]
    public async ValueTask<string> CreatePrefabsFromAllObjects()
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int prefabCount = 0;

            // Ensure Prefabs directory exists
            string prefabDir = "Assets/_Project/Prefabs";
            if (!Directory.Exists(prefabDir))
            {
                Directory.CreateDirectory(prefabDir);
                AssetDatabase.Refresh();
            }

            foreach (GameObject obj in allObjects)
            {
                // Skip Camera and Light objects
                if (obj.GetComponent<Camera>() != null ||
                    obj.GetComponent<Light>() != null ||
                    obj.name.Contains("Camera") ||
                    obj.name.Contains("Light"))
                {
                    continue;
                }

                string prefabPath = $"{prefabDir}/{obj.name}.prefab";

                // Save mesh as asset if it has MeshFilter
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    string meshPath = $"{prefabDir}/{obj.name}_Mesh.asset";
                    AssetDatabase.CreateAsset(meshFilter.sharedMesh, meshPath);
                }

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);

                if (prefab != null)
                {
                    prefabCount++;
                    Debug.Log($"Created prefab '{obj.name}' at {prefabPath}");
                }
            }

            AssetDatabase.Refresh();
            return $"Successfully created {prefabCount} prefabs in {prefabDir}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create prefabs: {e.Message}");
            throw;
        }
    }

    [McpServerTool, Description("List all prefabs in project")]
    public async ValueTask<string> ListPrefabs()
    {
        try
        {
            await UniTask.SwitchToMainThread();

            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
            if (prefabGUIDs.Length == 0)
            {
                return "No prefabs found in project";
            }

            string result = $"Found {prefabGUIDs.Length} prefabs:\n";
            foreach (string guid in prefabGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                result += $"- {name} ({path})\n";
            }

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to list prefabs: {e.Message}");
            throw;
        }
    }
}