using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEngine.UI;
// using TMPro; // Removed - using Unity standard UI Text instead

[McpServerToolType, Description("Comprehensive UI element creation tool for Canvas, Button, Text, Image, etc.")]
public class ComprehensiveUICreatorMCPTool
{
    [McpServerTool, Description("Create Canvas with specified settings")]
    public async ValueTask<string> CreateCanvas(
        [Description("Canvas name")] string name = "Canvas",
        [Description("Render mode: 0=ScreenSpaceOverlay, 1=ScreenSpaceCamera, 2=WorldSpace")] int renderMode = 0,
        [Description("Canvas sorting order")] int sortingOrder = 0)
    {
        await UniTask.SwitchToMainThread();

        var canvasGO = new GameObject(name);
        var canvas = canvasGO.AddComponent<Canvas>();
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        var raycaster = canvasGO.AddComponent<GraphicRaycaster>();

        canvas.renderMode = (RenderMode)renderMode;
        canvas.sortingOrder = sortingOrder;

        // Default CanvasScaler settings for responsive UI
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Debug.Log($"Canvas '{name}' created with render mode {(RenderMode)renderMode}");
        return $"SUCCESS: Canvas '{name}' created at position {canvasGO.transform.position}";
    }

    [McpServerTool, Description("Create Button with text and positioning")]
    public async ValueTask<string> CreateButton(
        [Description("Button name")] string name = "Button",
        [Description("Button text")] string buttonText = "Button",
        [Description("Parent Canvas name (optional)")] string parentCanvas = "",
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Width")] float width = 160f,
        [Description("Height")] float height = 30f)
    {
        await UniTask.SwitchToMainThread();

        GameObject parent = null;
        if (!string.IsNullOrEmpty(parentCanvas))
        {
            parent = GameObject.Find(parentCanvas);
            if (parent == null)
            {
                Debug.LogWarning($"Parent Canvas '{parentCanvas}' not found. Creating button without parent.");
            }
        }

        var buttonGO = new GameObject(name);
        if (parent != null)
        {
            buttonGO.transform.SetParent(parent.transform, false);
        }

        // Add Button components
        var image = buttonGO.AddComponent<Image>();
        var button = buttonGO.AddComponent<Button>();

        // Set RectTransform
        var rectTransform = buttonGO.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(x, y);
        rectTransform.sizeDelta = new Vector2(width, height);

        // Create text child
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        var text = textGO.AddComponent<Text>();
        text.text = buttonText;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Debug.Log($"Button '{name}' with text '{buttonText}' created");
        return $"SUCCESS: Button '{name}' created at position ({x}, {y}) with size ({width}, {height})";
    }

    [McpServerTool, Description("Create Text element with Unity UI Text")]
    public async ValueTask<string> CreateText(
        [Description("Text object name")] string name = "Text",
        [Description("Text content")] string textContent = "Sample Text",
        [Description("Parent Canvas name (optional)")] string parentCanvas = "",
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Font size")] float fontSize = 24f,
        [Description("Text color (hex format like #FFFFFF)")] string colorHex = "#000000")
    {
        await UniTask.SwitchToMainThread();

        GameObject parent = null;
        if (!string.IsNullOrEmpty(parentCanvas))
        {
            parent = GameObject.Find(parentCanvas);
        }

        var textGO = new GameObject(name);
        if (parent != null)
        {
            textGO.transform.SetParent(parent.transform, false);
        }

        var text = textGO.AddComponent<Text>();
        text.text = textContent;
        text.fontSize = (int)fontSize;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Parse color
        if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            text.color = color;
        }
        else
        {
            text.color = Color.black;
            Debug.LogWarning($"Invalid color format '{colorHex}', using black instead");
        }

        var rectTransform = textGO.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(x, y);

        Debug.Log($"Text '{name}' created with content '{textContent}'");
        return $"SUCCESS: Text '{name}' created at position ({x}, {y}) with font size {fontSize}";
    }

    [McpServerTool, Description("Create Image element")]
    public async ValueTask<string> CreateImage(
        [Description("Image object name")] string name = "Image",
        [Description("Parent Canvas name (optional)")] string parentCanvas = "",
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Width")] float width = 100f,
        [Description("Height")] float height = 100f,
        [Description("Image color (hex format like #FFFFFF)")] string colorHex = "#FFFFFF")
    {
        await UniTask.SwitchToMainThread();

        GameObject parent = null;
        if (!string.IsNullOrEmpty(parentCanvas))
        {
            parent = GameObject.Find(parentCanvas);
        }

        var imageGO = new GameObject(name);
        if (parent != null)
        {
            imageGO.transform.SetParent(parent.transform, false);
        }

        var image = imageGO.AddComponent<Image>();

        // Parse color
        if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            image.color = color;
        }
        else
        {
            image.color = Color.white;
            Debug.LogWarning($"Invalid color format '{colorHex}', using white instead");
        }

        var rectTransform = imageGO.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(x, y);
        rectTransform.sizeDelta = new Vector2(width, height);

        Debug.Log($"Image '{name}' created with color {color}");
        return $"SUCCESS: Image '{name}' created at position ({x}, {y}) with size ({width}, {height})";
    }

    [McpServerTool, Description("Create EventSystem if it doesn't exist")]
    public async ValueTask<string> CreateEventSystem()
    {
        await UniTask.SwitchToMainThread();

        var existingEventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existingEventSystem != null)
        {
            Debug.Log("EventSystem already exists");
            return "INFO: EventSystem already exists";
        }

        var eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        Debug.Log("EventSystem created");
        return "SUCCESS: EventSystem created for UI interaction";
    }

    [McpServerTool, Description("Create Panel (container for UI elements)")]
    public async ValueTask<string> CreatePanel(
        [Description("Panel name")] string name = "Panel",
        [Description("Parent Canvas name (optional)")] string parentCanvas = "",
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Width")] float width = 200f,
        [Description("Height")] float height = 200f,
        [Description("Background color (hex format like #FFFFFF)")] string colorHex = "#80808080")
    {
        await UniTask.SwitchToMainThread();

        GameObject parent = null;
        if (!string.IsNullOrEmpty(parentCanvas))
        {
            parent = GameObject.Find(parentCanvas);
        }

        var panelGO = new GameObject(name);
        if (parent != null)
        {
            panelGO.transform.SetParent(parent.transform, false);
        }

        var image = panelGO.AddComponent<Image>();

        // Parse color
        if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            image.color = color;
        }
        else
        {
            image.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            Debug.LogWarning($"Invalid color format '{colorHex}', using default gray instead");
        }

        var rectTransform = panelGO.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(x, y);
        rectTransform.sizeDelta = new Vector2(width, height);

        Debug.Log($"Panel '{name}' created with background color {color}");
        return $"SUCCESS: Panel '{name}' created at position ({x}, {y}) with size ({width}, {height})";
    }

    [McpServerTool, Description("Set RectTransform anchors for flexible UI layout")]
    public async ValueTask<string> SetRectTransformAnchors(
        [Description("Target GameObject name")] string objectName,
        [Description("Anchor Min X (0-1)")] float anchorMinX,
        [Description("Anchor Min Y (0-1)")] float anchorMinY,
        [Description("Anchor Max X (0-1)")] float anchorMaxX,
        [Description("Anchor Max Y (0-1)")] float anchorMaxY,
        [Description("Left offset")] float offsetLeft = 0f,
        [Description("Right offset")] float offsetRight = 0f,
        [Description("Top offset")] float offsetTop = 0f,
        [Description("Bottom offset")] float offsetBottom = 0f)
    {
        await UniTask.SwitchToMainThread();

        GameObject targetObject = GameObject.Find(objectName);
        if (targetObject == null)
        {
            return $"ERROR: GameObject '{objectName}' not found";
        }

        RectTransform rectTransform = targetObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return $"ERROR: GameObject '{objectName}' does not have a RectTransform component";
        }

        rectTransform.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rectTransform.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rectTransform.offsetMin = new Vector2(offsetLeft, offsetBottom);
        rectTransform.offsetMax = new Vector2(-offsetRight, -offsetTop);

        Debug.Log($"Set RectTransform anchors for '{objectName}': Min({anchorMinX}, {anchorMinY}), Max({anchorMaxX}, {anchorMaxY})");
        return $"SUCCESS: Set anchors for '{objectName}' - Min({anchorMinX}, {anchorMinY}), Max({anchorMaxX}, {anchorMaxY})";
    }

    [McpServerTool, Description("Set Image component color")]
    public async ValueTask<string> SetImageColor(
        [Description("Target GameObject name")] string objectName,
        [Description("Color in hex format (e.g., #B43232E6)")] string colorHex)
    {
        await UniTask.SwitchToMainThread();

        GameObject targetObject = GameObject.Find(objectName);
        if (targetObject == null)
        {
            return $"ERROR: GameObject '{objectName}' not found";
        }

        Image image = targetObject.GetComponent<Image>();
        if (image == null)
        {
            return $"ERROR: GameObject '{objectName}' does not have an Image component";
        }

        if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            image.color = color;
            Debug.Log($"Set Image color for '{objectName}': {colorHex} -> RGBA({color.r}, {color.g}, {color.b}, {color.a})");
            return $"SUCCESS: Set color for '{objectName}' to {colorHex}";
        }
        else
        {
            return $"ERROR: Invalid color format '{colorHex}'. Use format like #RRGGBB or #RRGGBBAA";
        }
    }
}