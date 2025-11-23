using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[McpServerToolType, Description("Create new work scenes for development")]
public class CreateSceneMCPTool
{
    [McpServerTool, Description("Create a new scene with basic setup for work")]
    public async ValueTask<string> CreateWorkScene(
        [Description("Scene name (without .unity extension)")] string sceneName = "WorkScene",
        [Description("Include directional light")] bool includeLight = true,
        [Description("Include camera")] bool includeCamera = true,
        [Description("Include ground plane")] bool includeGround = false)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Create new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Add basic objects based on parameters
            if (includeCamera)
            {
                var camera = new GameObject("Main Camera");
                camera.AddComponent<Camera>();
                camera.AddComponent<AudioListener>();
                camera.transform.position = new Vector3(0, 1, -10);
                camera.tag = "MainCamera";
            }

            if (includeLight)
            {
                var light = new GameObject("Directional Light");
                var lightComponent = light.AddComponent<Light>();
                lightComponent.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            if (includeGround)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.localScale = new Vector3(10, 1, 10);
            }

            // Save scene
            string scenePath = $"Assets/_Project/Scenes/{sceneName}.unity";
            
            // Create Scenes directory if it doesn't exist
            string scenesDir = "Assets/_Project/Scenes";
            if (!Directory.Exists(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
                AssetDatabase.Refresh();
            }

            bool saved = EditorSceneManager.SaveScene(newScene, scenePath);
            
            if (saved)
            {
                Debug.Log($"Work scene '{sceneName}' created successfully at {scenePath}");
                return $"Successfully created work scene '{sceneName}' with camera: {includeCamera}, light: {includeLight}, ground: {includeGround}";
            }
            else
            {
                throw new Exception("Failed to save scene");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create work scene: {e.Message}");
            throw;
        }
    }

    [McpServerTool, Description("Load an existing scene by name")]
    public async ValueTask<string> LoadScene(
        [Description("Scene name (with or without .unity extension)")] string sceneName)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Add .unity extension if not present
            if (!sceneName.EndsWith(".unity"))
            {
                sceneName += ".unity";
            }

            string scenePath = $"Assets/_Project/Scenes/{sceneName}";
            
            if (!File.Exists(scenePath))
            {
                throw new Exception($"Scene not found at {scenePath}");
            }

            EditorSceneManager.OpenScene(scenePath);
            
            Debug.Log($"Loaded scene: {sceneName}");
            return $"Successfully loaded scene: {sceneName}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load scene: {e.Message}");
            throw;
        }
    }

    [McpServerTool, Description("List all available scenes in the project")]
    public async ValueTask<string> ListScenes()
    {
        try
        {
            await UniTask.SwitchToMainThread();

            string scenesDir = "Assets/_Project/Scenes";
            
            if (!Directory.Exists(scenesDir))
            {
                return "No scenes directory found. Create a scene first to initialize the directory.";
            }

            string[] sceneFiles = Directory.GetFiles(scenesDir, "*.unity");
            
            if (sceneFiles.Length == 0)
            {
                return "No scenes found in Assets/_Project/Scenes/";
            }

            string sceneList = "Available scenes:\n";
            foreach (string scenePath in sceneFiles)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                sceneList += $"- {sceneName}\n";
            }

            Debug.Log(sceneList);
            return sceneList.Trim();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to list scenes: {e.Message}");
            throw;
        }
    }
}