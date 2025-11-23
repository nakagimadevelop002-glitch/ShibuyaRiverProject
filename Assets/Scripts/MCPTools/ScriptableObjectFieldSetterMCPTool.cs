using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEditor;

[McpServerToolType, Description("Set field values on ScriptableObject assets")]
public class ScriptableObjectFieldSetterMCPTool
{
    [McpServerTool, Description("Set any field value on a ScriptableObject (auto-parses primitives, string, enum, Color, Vector2/3/4)")]
    public async ValueTask<string> SetFieldValue(
        [Description("Asset path (e.g., Assets/_Project/Data/Card.asset)")] string assetPath,
        [Description("Field name")] string fieldName,
        [Description("Value as string (will be parsed to appropriate type)")] string value)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null) return $"ERROR: Asset not found at {assetPath}";

            var field = asset.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) return $"ERROR: Field '{fieldName}' not found";

            // Parse value based on field type
            object parsedValue = ParseValue(field.FieldType, value);
            if (parsedValue == null && !string.IsNullOrEmpty(value))
            {
                return $"ERROR: Failed to parse '{value}' as {field.FieldType.Name}";
            }

            field.SetValue(asset, parsedValue);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return $"SUCCESS: Set {fieldName} = {parsedValue}";
        }
        catch (Exception e) { return $"ERROR: {e.Message}"; }
#else
        await UniTask.Yield();
        return "ERROR: Editor only";
#endif
    }

    private object ParseValue(Type targetType, string value)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (targetType == typeof(bool))
            {
                if (value.ToLower() == "true" || value == "1") return true;
                if (value.ToLower() == "false" || value == "0") return false;
                return bool.Parse(value);
            }

            if (targetType == typeof(int)) return int.Parse(value);
            if (targetType == typeof(float)) return float.Parse(value);
            if (targetType == typeof(double)) return double.Parse(value);
            if (targetType == typeof(long)) return long.Parse(value);
            if (targetType == typeof(string)) return value;

            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, true);

            if (targetType == typeof(Color))
            {
                Color color;
                if (ColorUtility.TryParseHtmlString(value, out color))
                    return color;
                return Color.white;
            }

            if (targetType == typeof(Vector2))
            {
                string[] parts = value.Split(',');
                if (parts.Length == 2)
                    return new Vector2(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()));
            }

            if (targetType == typeof(Vector3))
            {
                string[] parts = value.Split(',');
                if (parts.Length == 3)
                    return new Vector3(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()), float.Parse(parts[2].Trim()));
            }

            if (targetType == typeof(Vector4))
            {
                string[] parts = value.Split(',');
                if (parts.Length == 4)
                    return new Vector4(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()), float.Parse(parts[2].Trim()), float.Parse(parts[3].Trim()));
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    [McpServerTool, Description("Get all field names and current values from a ScriptableObject")]
    public async ValueTask<string> GetFields(
        [Description("Asset path")] string assetPath)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null) return $"ERROR: Asset not found at {assetPath}";

            var fields = asset.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            string result = $"Fields in {asset.GetType().Name}:\n";
            foreach (var field in fields)
            {
                var value = field.GetValue(asset);
                string valueStr = value != null ? value.ToString() : "null";
                result += $"- {field.Name} ({field.FieldType.Name}): {valueStr}\n";
            }
            return result.Trim();
        }
        catch (Exception e) { return $"ERROR: {e.Message}"; }
#else
        await UniTask.Yield();
        return "ERROR: Editor only";
#endif
    }
}
