using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("Capture screenshots for visual verification")]
public class ScreenshotCaptureMCPTool
{
    [McpServerTool, Description("Capture Game View screenshot")]
    public async ValueTask<string> CaptureGameView(
        [Description("Screenshot filename (without extension)")] string filename = "GameView_Screenshot",
        [Description("Save to project root if true, otherwise to Assets folder")] bool saveToProjectRoot = true)
    {
#if UNITY_EDITOR
        await UniTask.SwitchToMainThread();

        try
        {
            // Determine save path
            string folderPath = saveToProjectRoot
                ? System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath)
                : UnityEngine.Application.dataPath;

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fullFilename = $"{filename}_{timestamp}.png";
            string fullPath = System.IO.Path.Combine(folderPath, fullFilename);

            // Get Game View using reflection
            var gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var gameView = UnityEditor.EditorWindow.GetWindow(gameViewType);

            if (gameView == null)
            {
                return "ERROR: Game View window not found. Please open Game View window first.";
            }

            // Focus Game View to ensure it's rendered
            gameView.Focus();
            await UniTask.Delay(100); // Wait for render

            // Capture screenshot
            UnityEngine.ScreenCapture.CaptureScreenshot(fullPath);

            // Wait for file to be written
            await UniTask.Delay(500);

            Debug.Log($"Screenshot saved: {fullPath}");
            return $"SUCCESS: Screenshot saved to '{fullPath}'";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Screenshot capture failed: {e.Message}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: Screenshot capture only available in Unity Editor";
#endif
    }

    [McpServerTool, Description("Capture Scene View screenshot")]
    public async ValueTask<string> CaptureSceneView(
        [Description("Screenshot filename (without extension)")] string filename = "SceneView_Screenshot",
        [Description("Save to project root if true, otherwise to Assets folder")] bool saveToProjectRoot = true,
        [Description("Screenshot width")] int width = 1920,
        [Description("Screenshot height")] int height = 1080)
    {
#if UNITY_EDITOR
        await UniTask.SwitchToMainThread();

        try
        {
            // Determine save path
            string folderPath = saveToProjectRoot
                ? System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath)
                : UnityEngine.Application.dataPath;

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fullFilename = $"{filename}_{timestamp}.png";
            string fullPath = System.IO.Path.Combine(folderPath, fullFilename);

            // Get Scene View using reflection
            var sceneViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneView");
            var sceneView = UnityEditor.EditorWindow.GetWindow(sceneViewType);

            if (sceneView == null)
            {
                return "ERROR: Scene View window not found. Please open Scene View window first.";
            }

            // Focus Scene View
            sceneView.Focus();
            await UniTask.Delay(100);

            // Get Scene View camera using reflection
            var cameraProperty = sceneViewType.GetProperty("camera");
            Camera sceneCamera = cameraProperty.GetValue(sceneView) as Camera;

            if (sceneCamera == null)
            {
                return "ERROR: Scene View camera not found.";
            }

            // Create RenderTexture
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            RenderTexture previousTarget = sceneCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            // Render Scene View to texture
            sceneCamera.targetTexture = renderTexture;
            sceneCamera.Render();

            // Read pixels from RenderTexture
            RenderTexture.active = renderTexture;
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            // Restore previous state
            sceneCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            // Save to file
            byte[] bytes = screenshot.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, bytes);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(screenshot);

            Debug.Log($"Scene View screenshot saved: {fullPath}");
            return $"SUCCESS: Screenshot saved to '{fullPath}'";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Screenshot capture failed: {e.Message}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: Screenshot capture only available in Unity Editor";
#endif
    }
}
