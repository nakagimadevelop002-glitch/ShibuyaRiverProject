using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[McpServerToolType, Description("Manage GameObjects in the scene")]
public class GameObjectManagerMCPTool
{
    [McpServerTool, Description("Delete a GameObject by name")]
    public async ValueTask<string> DeleteGameObject(
        [Description("GameObject name to delete")] string objectName)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return $"GameObject '{objectName}' not found";
            }

            GameObject.DestroyImmediate(obj);
            Debug.Log($"Deleted GameObject '{objectName}'");

#if UNITY_EDITOR
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved automatically");
#endif

            return $"Successfully deleted GameObject '{objectName}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete GameObject: {e.Message}");
            throw;
        }
    }

    [McpServerTool, Description("Clear all GameObjects except Camera and Light")]
    public async ValueTask<string> ClearScene()
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int deletedCount = 0;

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

                GameObject.DestroyImmediate(obj);
                deletedCount++;
            }

            Debug.Log($"Cleared scene: deleted {deletedCount} objects");

#if UNITY_EDITOR
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved automatically");
#endif

            return $"Successfully cleared scene: deleted {deletedCount} objects";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to clear scene: {e.Message}");
            throw;
        }
    }

    [McpServerTool, Description("Set parent of a GameObject")]
    public async ValueTask<string> SetParent(
        [Description("Child GameObject name")] string childName,
        [Description("Parent GameObject name")] string parentName)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject child = GameObject.Find(childName);
            if (child == null)
            {
                return $"ERROR: Child GameObject '{childName}' not found";
            }

            GameObject parent = GameObject.Find(parentName);
            if (parent == null)
            {
                return $"ERROR: Parent GameObject '{parentName}' not found";
            }

            child.transform.SetParent(parent.transform, false);
            Debug.Log($"Set '{childName}' as child of '{parentName}'");

#if UNITY_EDITOR
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved automatically");
#endif

            return $"SUCCESS: Set '{childName}' as child of '{parentName}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set parent: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }

    [McpServerTool, Description("Rename a GameObject")]
    public async ValueTask<string> RenameGameObject(
        [Description("Current GameObject name")] string oldName,
        [Description("New GameObject name")] string newName)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject obj = GameObject.Find(oldName);
            if (obj == null)
            {
                return $"ERROR: GameObject '{oldName}' not found";
            }

            obj.name = newName;
            Debug.Log($"Renamed GameObject '{oldName}' to '{newName}'");

#if UNITY_EDITOR
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved automatically");
#endif

            return $"SUCCESS: Renamed GameObject '{oldName}' to '{newName}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to rename GameObject: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }
}