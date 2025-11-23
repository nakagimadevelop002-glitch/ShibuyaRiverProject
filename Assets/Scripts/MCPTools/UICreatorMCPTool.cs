using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEngine.UI;

[McpServerToolType, Description("Create Unity UI elements programmatically")]
public class UICreatorMCPTool
{
    [McpServerTool, Description("Create a Canvas with specified name")]
    public async ValueTask<string> CreateCanvas(
        [Description("Canvas name")] string canvasName = "GameCanvas")
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Create Canvas GameObject
            GameObject canvasObject = new GameObject(canvasName);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            GraphicRaycaster graphicRaycaster = canvasObject.AddComponent<GraphicRaycaster>();

            // Configure Canvas
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            // Configure CanvasScaler
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0f;

            Debug.Log($"Created Canvas: {canvasName}");
            return $"SUCCESS: Created Canvas '{canvasName}' with CanvasScaler and GraphicRaycaster";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create Canvas: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }

    [McpServerTool, Description("Create a Panel under specified parent")]
    public async ValueTask<string> CreatePanel(
        [Description("Parent object name")] string parentName,
        [Description("Panel name")] string panelName,
        [Description("X position")] float posX = 0f,
        [Description("Y position")] float posY = 0f)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Find parent object
            GameObject parentObject = GameObject.Find(parentName);
            if (parentObject == null)
            {
                return $"ERROR: Parent object '{parentName}' not found";
            }

            // Create Panel GameObject
            GameObject panelObject = new GameObject(panelName);
            panelObject.transform.SetParent(parentObject.transform);

            // Add RectTransform and configure
            RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = new Vector2(posX, posY);

            // Add Image component for panel background
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(1f, 1f, 1f, 0.1f); // Semi-transparent white

            Debug.Log($"Created Panel: {panelName} under {parentName} at ({posX}, {posY})");
            return $"SUCCESS: Created Panel '{panelName}' under '{parentName}' at position ({posX}, {posY})";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create Panel: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }

    [McpServerTool, Description("Create a Button under specified parent")]
    public async ValueTask<string> CreateButton(
        [Description("Parent object name")] string parentName,
        [Description("Button name")] string buttonName,
        [Description("Button text")] string buttonText = "Button",
        [Description("X position")] float posX = 0f,
        [Description("Y position")] float posY = 0f,
        [Description("Width")] float width = 160f,
        [Description("Height")] float height = 30f)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Find parent object
            GameObject parentObject = GameObject.Find(parentName);
            if (parentObject == null)
            {
                return $"ERROR: Parent object '{parentName}' not found";
            }

            // Create Button GameObject
            GameObject buttonObject = new GameObject(buttonName);
            buttonObject.transform.SetParent(parentObject.transform);

            // Add RectTransform and configure
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(posX, posY);
            rectTransform.sizeDelta = new Vector2(width, height);

            // Add Image and Button components
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = Color.white;
            Button button = buttonObject.AddComponent<Button>();

            // Create Text child for button label
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObject.AddComponent<Text>();
            text.text = buttonText;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;

            Debug.Log($"Created Button: {buttonName} with text '{buttonText}'");
            return $"SUCCESS: Created Button '{buttonName}' with text '{buttonText}' at ({posX}, {posY})";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create Button: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }

    [McpServerTool, Description("Create a Text element under specified parent")]
    public async ValueTask<string> CreateText(
        [Description("Parent object name")] string parentName,
        [Description("Text object name")] string textName,
        [Description("Text content")] string textContent = "Text",
        [Description("Font size")] int fontSize = 14,
        [Description("X position")] float posX = 0f,
        [Description("Y position")] float posY = 0f)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Find parent object
            GameObject parentObject = GameObject.Find(parentName);
            if (parentObject == null)
            {
                return $"ERROR: Parent object '{parentName}' not found";
            }

            // Create Text GameObject
            GameObject textObject = new GameObject(textName);
            textObject.transform.SetParent(parentObject.transform);

            // Add RectTransform and configure
            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(posX, posY);
            rectTransform.sizeDelta = new Vector2(200f, 50f);

            // Add Text component
            Text text = textObject.AddComponent<Text>();
            text.text = textContent;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;

            Debug.Log($"Created Text: {textName} with content '{textContent}'");
            return $"SUCCESS: Created Text '{textName}' with content '{textContent}' at ({posX}, {posY})";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create Text: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }

    [McpServerTool, Description("Create EventSystem if it doesn't exist")]
    public async ValueTask<string> CreateEventSystem()
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Check if EventSystem already exists
            UnityEngine.EventSystems.EventSystem existingEventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem != null)
            {
                return "INFO: EventSystem already exists in scene";
            }

            // Create EventSystem GameObject
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("Created EventSystem with StandaloneInputModule");
            return "SUCCESS: Created EventSystem with StandaloneInputModule";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create EventSystem: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }
}