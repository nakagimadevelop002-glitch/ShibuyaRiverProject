using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[McpServerToolType, Description("Instantiate prefab in scene")]
public class InstantiatePrefabMCPTool
{
    [McpServerTool, Description("Instantiate a prefab at specified position")]
    public async ValueTask<string> InstantiatePrefab(
        [Description("Prefab path (e.g., 'Assets/Prefab/RiverWalker.prefab')")] string prefabPath,
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Z position")] float z = 0f,
        [Description("Optional: New GameObject name (if empty, use prefab name)")] string newName = "")
    {
        await UniTask.SwitchToMainThread();

#if UNITY_EDITOR
        // Load prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            return $"ERROR: Prefab not found at path '{prefabPath}'";
        }

        // Instantiate prefab
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instance == null)
        {
            return $"ERROR: Failed to instantiate prefab '{prefabPath}'";
        }

        // Set position
        instance.transform.position = new Vector3(x, y, z);

        // Set name if specified
        if (!string.IsNullOrEmpty(newName))
        {
            instance.name = newName;
        }

        // Mark scene dirty and save
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"Instantiated prefab '{instance.name}' at ({x}, {y}, {z})");
        return $"SUCCESS: Instantiated '{instance.name}' at position ({x}, {y}, {z})";
#else
        return "ERROR: This tool only works in Unity Editor";
#endif
    }
}
