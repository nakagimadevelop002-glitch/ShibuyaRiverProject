using System;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[McpServerToolType, System.ComponentModel.Description("Attach Unity components to GameObjects")]
public class ComponentAttachMCPTool
{
    [McpServerTool, System.ComponentModel.Description("Attach a component to a specific GameObject by name")]
    public async ValueTask<string> AttachScriptToObject(
        [System.ComponentModel.Description("Target GameObject name")] string objectName,
        [System.ComponentModel.Description("Component class name (any Component type)")] string scriptClassName)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            Type scriptType = Type.GetType(scriptClassName);
            if (scriptType == null)
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    scriptType = assembly.GetType(scriptClassName);
                    if (scriptType != null) break;
                }
            }

            if (scriptType == null)
            {
                return $"ERROR: Script class '{scriptClassName}' not found";
            }

            // Component型チェック（MonoBehaviourだけでなく全Component対応）
            if (!typeof(UnityEngine.Component).IsAssignableFrom(scriptType))
            {
                return $"ERROR: '{scriptClassName}' is not a Component type";
            }

            UnityEngine.Component existingComponent = obj.GetComponent(scriptType);
            if (existingComponent != null)
            {
                return $"INFO: Component '{scriptClassName}' already exists on '{objectName}'";
            }

            UnityEngine.Component component = obj.AddComponent(scriptType);
            Debug.Log($"Attached component '{scriptClassName}' to GameObject '{objectName}'");

#if UNITY_EDITOR
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved after attaching script");
#endif

            return $"SUCCESS: Attached script '{scriptClassName}' to '{objectName}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to attach script: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }

    [McpServerTool, System.ComponentModel.Description("Attach a component to all GameObjects with a specific tag")]
    public async ValueTask<string> AttachScriptToObjectsWithTag(
        [System.ComponentModel.Description("Target tag name")] string tag,
        [System.ComponentModel.Description("Component class name (any Component type)")] string scriptClassName)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            if (objects.Length == 0)
            {
                return $"ERROR: No GameObjects found with tag '{tag}'";
            }

            Type scriptType = Type.GetType(scriptClassName);
            if (scriptType == null)
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    scriptType = assembly.GetType(scriptClassName);
                    if (scriptType != null) break;
                }
            }

            if (scriptType == null)
            {
                return $"ERROR: Script class '{scriptClassName}' not found";
            }

            // Component型チェック（MonoBehaviourだけでなく全Component対応）
            if (!typeof(UnityEngine.Component).IsAssignableFrom(scriptType))
            {
                return $"ERROR: '{scriptClassName}' is not a Component type";
            }

            int attachedCount = 0;
            foreach (GameObject obj in objects)
            {
                UnityEngine.Component existingComponent = obj.GetComponent(scriptType);
                if (existingComponent == null)
                {
                    obj.AddComponent(scriptType);
                    attachedCount++;
                }
            }

            Debug.Log($"Attached script '{scriptClassName}' to {attachedCount} objects with tag '{tag}'");

#if UNITY_EDITOR
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved after attaching scripts");
#endif

            return $"SUCCESS: Attached script '{scriptClassName}' to {attachedCount} objects with tag '{tag}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to attach script to objects with tag: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }

    [McpServerTool, System.ComponentModel.Description("Remove a specific component from a GameObject")]
    public async ValueTask<string> RemoveScriptFromObject(
        [System.ComponentModel.Description("Target GameObject name")] string objectName,
        [System.ComponentModel.Description("Script class name to remove")] string scriptClassName)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            Type scriptType = Type.GetType(scriptClassName);
            if (scriptType == null)
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    scriptType = assembly.GetType(scriptClassName);
                    if (scriptType != null) break;
                }
            }

            if (scriptType == null)
            {
                return $"ERROR: Script class '{scriptClassName}' not found";
            }

            UnityEngine.Component component = obj.GetComponent(scriptType);
            if (component == null)
            {
                return $"INFO: Component '{scriptClassName}' not found on '{objectName}'";
            }

            UnityEngine.Component.DestroyImmediate(component);
            Debug.Log($"Removed script '{scriptClassName}' from GameObject '{objectName}'");

#if UNITY_EDITOR
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved after removing script");
#endif

            return $"SUCCESS: Removed script '{scriptClassName}' from '{objectName}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to remove script: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }

    [McpServerTool, System.ComponentModel.Description("List all available Component types in the project")]
    public async ValueTask<string> ListAvailableScripts()
    {
        try
        {
            await UniTask.SwitchToMainThread();

            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            var scripts = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(UnityEngine.Component).IsAssignableFrom(type) && !type.IsAbstract)
                .OrderBy(type => type.Name)
                .ToList();

            if (scripts.Count == 0)
            {
                return "No Component types found";
            }

            string result = $"Found {scripts.Count} Component types:\n";
            foreach (Type script in scripts)
            {
                result += $"- {script.Name} ({script.Namespace})\n";
            }

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to list components: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }
}