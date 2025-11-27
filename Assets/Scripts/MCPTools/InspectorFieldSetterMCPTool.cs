using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEngine.Rendering;

[McpServerToolType, Description("Set Inspector field values on GameObjects with reflection")]
public class InspectorFieldSetterMCPTool
{
    [McpServerTool, Description("Set a field on a component by GameObject name (supports public and [SerializeField] private fields)")]
    public async ValueTask<string> SetComponentField(
        [Description("Target GameObject name")] string objectName,
        [Description("Component type name (e.g., GameManager)")] string componentTypeName,
        [Description("Field name to set")] string fieldName,
        [Description("Value GameObject name (for Transform/GameObject references)")] string valueObjectName)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Find target GameObject
            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            // Find component type
            var componentType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == componentTypeName && typeof(UnityEngine.Component).IsAssignableFrom(t));

            if (componentType == null)
            {
                return $"ERROR: Component type '{componentTypeName}' not found";
            }

            // Get component
            var component = targetObject.GetComponent(componentType);
            if (component == null)
            {
                return $"ERROR: Component '{componentTypeName}' not found on '{objectName}'";
            }

            // Find field or property (support both public and private [SerializeField] fields)
            FieldInfo field = componentType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo property = null;
            Type memberType = null;

            if (field != null)
            {
                memberType = field.FieldType;
            }
            else
            {
                property = componentType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null)
                {
                    return $"ERROR: Field or property '{fieldName}' not found on '{componentTypeName}'";
                }
                memberType = property.PropertyType;
            }

            // Find value GameObject
            GameObject valueObject = GameObject.Find(valueObjectName);
            if (valueObject == null)
            {
                return $"ERROR: Value GameObject '{valueObjectName}' not found";
            }

            // Determine value based on member type
            object valueToSet = null;

            if (memberType == typeof(Transform))
            {
                valueToSet = valueObject.transform;
            }
            else if (memberType == typeof(GameObject))
            {
                valueToSet = valueObject;
            }
            else if (typeof(UnityEngine.Component).IsAssignableFrom(memberType))
            {
                var valueComponent = valueObject.GetComponent(memberType);
                if (valueComponent == null)
                {
                    return $"ERROR: Component '{memberType.Name}' not found on '{valueObjectName}'";
                }
                valueToSet = valueComponent;
            }
            else
            {
                return $"ERROR: Unsupported member type '{memberType.Name}'";
            }

            // Set value (field or property)
            if (field != null)
            {
                field.SetValue(component, valueToSet);
            }
            else
            {
                property.SetValue(component, valueToSet);
            }

#if UNITY_EDITOR
            // Mark component and scene dirty, then save
            UnityEditor.EditorUtility.SetDirty(component);
            UnityEditor.EditorUtility.SetDirty(targetObject);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved after setting {componentTypeName}.{fieldName} = {valueObjectName}");
#endif

            Debug.Log($"Set {componentTypeName}.{fieldName} = {valueObjectName}");
            return $"SUCCESS: Set '{componentTypeName}.{fieldName}' to '{valueObjectName}' on '{objectName}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set field: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }

    [McpServerTool, Description("Set a GameObject/Transform array field on a component (Inspector operation)")]
    public async ValueTask<string> SetComponentFieldArray(
        [Description("Target GameObject name")] string objectName,
        [Description("Component type name (e.g., RouteFollower)")] string componentTypeName,
        [Description("Field name to set")] string fieldName,
        [Description("Comma-separated GameObject names (e.g., 'Waypoint1,Waypoint2,Waypoint3')")] string valueObjectNames)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Find target GameObject
            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            // Find component type
            var componentType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == componentTypeName && typeof(UnityEngine.Component).IsAssignableFrom(t));

            if (componentType == null)
            {
                return $"ERROR: Component type '{componentTypeName}' not found";
            }

            // Get component
            var component = targetObject.GetComponent(componentType);
            if (component == null)
            {
                return $"ERROR: Component '{componentTypeName}' not found on '{objectName}'";
            }

            // Find field (support both public and private [SerializeField] fields)
            FieldInfo field = componentType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                return $"ERROR: Field '{fieldName}' not found on '{componentTypeName}'";
            }

            // Parse GameObject names
            string[] names = valueObjectNames.Split(',');
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = names[i].Trim();
            }

            // Determine array element type
            Type fieldType = field.FieldType;
            if (!fieldType.IsArray)
            {
                return $"ERROR: Field '{fieldName}' is not an array type";
            }

            Type elementType = fieldType.GetElementType();

            // Create array based on element type
            if (elementType == typeof(GameObject))
            {
                GameObject[] gameObjects = new GameObject[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    GameObject obj = GameObject.Find(names[i]);
                    if (obj == null)
                    {
                        return $"ERROR: GameObject '{names[i]}' not found";
                    }
                    gameObjects[i] = obj;
                }
                field.SetValue(component, gameObjects);
            }
            else if (elementType == typeof(Transform))
            {
                Transform[] transforms = new Transform[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    GameObject obj = GameObject.Find(names[i]);
                    if (obj == null)
                    {
                        return $"ERROR: GameObject '{names[i]}' not found";
                    }
                    transforms[i] = obj.transform;
                }
                field.SetValue(component, transforms);
            }
            else
            {
                return $"ERROR: Unsupported array element type '{elementType.Name}'";
            }

#if UNITY_EDITOR
            // Mark component and scene dirty, then save
            UnityEditor.EditorUtility.SetDirty(component);
            UnityEditor.EditorUtility.SetDirty(targetObject);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved after setting {componentTypeName}.{fieldName} = [{valueObjectNames}]");
#endif

            Debug.Log($"Set {componentTypeName}.{fieldName} = [{valueObjectNames}]");
            return $"SUCCESS: Set '{componentTypeName}.{fieldName}' to [{valueObjectNames}] on '{objectName}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set array field: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }

    [McpServerTool, Description("Set a ScriptableObject asset field on a component")]
    public async ValueTask<string> SetScriptableObjectField(
        [Description("Target GameObject name")] string objectName,
        [Description("Component type name (e.g., GameManager)")] string componentTypeName,
        [Description("Field name to set")] string fieldName,
        [Description("ScriptableObject asset path or name")] string assetPath)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();

            // Find target GameObject
            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            // Find component type
            var componentType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == componentTypeName && typeof(UnityEngine.Component).IsAssignableFrom(t));

            if (componentType == null)
            {
                return $"ERROR: Component type '{componentTypeName}' not found";
            }

            // Get component
            var component = targetObject.GetComponent(componentType);
            if (component == null)
            {
                return $"ERROR: Component '{componentTypeName}' not found on '{objectName}'";
            }

            // Find field
            FieldInfo field = componentType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                return $"ERROR: Field '{fieldName}' not found on '{componentTypeName}'";
            }

            // Load ScriptableObject asset using reflection
            var assetDatabaseType = typeof(UnityEditor.AssetDatabase);

            // Ensure path starts with "Assets/"
            if (!assetPath.StartsWith("Assets/") && assetPath.Contains("/"))
            {
                assetPath = "Assets/" + assetPath;
            }

            // Try to load by path first - get generic LoadAssetAtPath<T>(string path) method
            var loadAssetAtPathMethods = assetDatabaseType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "LoadAssetAtPath" &&
                           m.IsGenericMethod &&
                           m.GetParameters().Length == 1 &&
                           m.GetParameters()[0].ParameterType == typeof(string));

            if (!loadAssetAtPathMethods.Any())
            {
                return "ERROR: LoadAssetAtPath method not found";
            }

            var loadAssetAtPathMethod = loadAssetAtPathMethods.First().MakeGenericMethod(field.FieldType);
            var asset = loadAssetAtPathMethod.Invoke(null, new object[] { assetPath });

            // If not found, try to find by name
            if (asset == null)
            {
                var findAssetsMethod = assetDatabaseType.GetMethod("FindAssets",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);
                var guidToAssetPathMethod = assetDatabaseType.GetMethod("GUIDToAssetPath",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);

                string[] guids = (string[])findAssetsMethod.Invoke(null, new object[] { assetPath });
                if (guids.Length > 0)
                {
                    string path = (string)guidToAssetPathMethod.Invoke(null, new object[] { guids[0] });
                    asset = loadAssetAtPathMethod.Invoke(null, new object[] { path });
                }
            }

            if (asset == null)
            {
                return $"ERROR: ScriptableObject asset '{assetPath}' not found";
            }

            // Set field
            field.SetValue(component, asset);

            Debug.Log($"Set {componentTypeName}.{fieldName} = {assetPath}");
            return $"SUCCESS: Set '{componentTypeName}.{fieldName}' to '{assetPath}' on '{objectName}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set ScriptableObject field: {e.Message}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: ScriptableObject field setting only available in Unity Editor";
#endif
    }

    [McpServerTool, Description("Set any field value on a component (auto-parses primitives, string, enum, Color, Vector2/3/4, supports nested fields with dot notation)")]
    public async ValueTask<string> SetFieldValue(
        [Description("Target GameObject name")] string objectName,
        [Description("Component type name (e.g., ModalSystem)")] string componentTypeName,
        [Description("Field name to set (supports nested fields like 'profile.intensity.value' or 'components[0].fixedExposure')")] string fieldName,
        [Description("Value as string (will be parsed to appropriate type)")] string value)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            var componentType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == componentTypeName && typeof(UnityEngine.Component).IsAssignableFrom(t));

            if (componentType == null)
            {
                return $"ERROR: Component type '{componentTypeName}' not found";
            }

            var component = targetObject.GetComponent(componentType);
            if (component == null)
            {
                return $"ERROR: Component '{componentTypeName}' not found on '{objectName}'";
            }

            // ネストされたフィールドパスを解析
            var pathResult = ResolveNestedFieldPath(component, componentType, fieldName);
            if (pathResult.errorMessage != null)
            {
                return pathResult.errorMessage;
            }

            // Parse value based on member type
            object parsedValue = ParseValue(pathResult.finalType, value);
            if (parsedValue == null && !string.IsNullOrEmpty(value))
            {
                return $"ERROR: Failed to parse '{value}' as {pathResult.finalType.Name}";
            }

            // Set value
            if (pathResult.finalField != null)
            {
                pathResult.finalField.SetValue(pathResult.finalObject, parsedValue);
            }
            else if (pathResult.finalProperty != null)
            {
                pathResult.finalProperty.SetValue(pathResult.finalObject, parsedValue);
            }

            // ScriptableObjectへの変更を検知して保存
            // ネストされたパスの途中にScriptableObjectがあればそれもDirtyにする
            MarkDirtyAlongPath(component, componentType, fieldName);

            // Mark scene dirty and save
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            // ScriptableObjectアセットを保存
            UnityEditor.AssetDatabase.SaveAssets();

            // アセットデータベースをリフレッシュ（変更を確実に反映）
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"Scene '{scene.name}' saved after setting {componentTypeName}.{fieldName} = {parsedValue}");

            return $"SUCCESS: Set {objectName}.{componentTypeName}.{fieldName} = {parsedValue}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set field value: {e.Message}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: Field value setting only available in Unity Editor";
#endif
    }

    /// <summary>
    /// ネストされたパスを辿ってScriptableObjectをDirtyマークする
    /// </summary>
    private void MarkDirtyAlongPath(object rootObject, Type rootType, string path)
    {
        object currentObject = rootObject;
        Type currentType = rootType;

        // ルートオブジェクトがScriptableObjectならDirtyマーク
        if (currentObject is ScriptableObject)
        {
            UnityEditor.EditorUtility.SetDirty((ScriptableObject)currentObject);
        }

        string[] pathParts = path.Split('.');

        for (int i = 0; i < pathParts.Length - 1; i++) // 最後の要素は除く
        {
            string part = pathParts[i];
            string fieldName = part;
            int arrayIndex = -1;

            if (part.Contains("[") && part.EndsWith("]"))
            {
                int bracketStart = part.IndexOf('[');
                fieldName = part.Substring(0, bracketStart);
                string indexStr = part.Substring(bracketStart + 1, part.Length - bracketStart - 2);
                int.TryParse(indexStr, out arrayIndex);
            }

            FieldInfo field = currentType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo property = null;
            object nextObject = null;

            if (field != null)
            {
                nextObject = field.GetValue(currentObject);
            }
            else
            {
                property = currentType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanRead)
                {
                    nextObject = property.GetValue(currentObject);
                }
            }

            if (nextObject == null) break;

            // 配列/リストの場合
            if (arrayIndex >= 0)
            {
                if (nextObject is Array)
                {
                    Array array = (Array)nextObject;
                    if (arrayIndex >= 0 && arrayIndex < array.Length)
                    {
                        nextObject = array.GetValue(arrayIndex);
                    }
                }
                else if (nextObject is System.Collections.IList)
                {
                    var list = nextObject as System.Collections.IList;
                    if (arrayIndex >= 0 && arrayIndex < list.Count)
                    {
                        nextObject = list[arrayIndex];
                    }
                }
            }

            // ScriptableObjectならDirtyマーク
            if (nextObject is ScriptableObject)
            {
                UnityEditor.EditorUtility.SetDirty((ScriptableObject)nextObject);
            }

            currentObject = nextObject;
            currentType = nextObject?.GetType();
            if (currentType == null) break;
        }
    }

    /// <summary>
    /// ネストされたフィールドパスを解析して最終的なフィールド/プロパティを取得
    /// 例: "profile.intensity.value" や "components[0].fixedExposure"
    /// </summary>
    private (object finalObject, FieldInfo finalField, PropertyInfo finalProperty, Type finalType, string errorMessage)
        ResolveNestedFieldPath(object rootObject, Type rootType, string path)
    {
        object currentObject = rootObject;
        Type currentType = rootType;

        // パスをドットで分割（配列インデックスは保持）
        string[] pathParts = path.Split('.');

        for (int i = 0; i < pathParts.Length; i++)
        {
            string part = pathParts[i];
            bool isLastPart = (i == pathParts.Length - 1);

            // 配列/リストインデックスアクセスを処理（例: "components[0]"）
            string fieldName = part;
            int arrayIndex = -1;

            if (part.Contains("[") && part.EndsWith("]"))
            {
                int bracketStart = part.IndexOf('[');
                fieldName = part.Substring(0, bracketStart);
                string indexStr = part.Substring(bracketStart + 1, part.Length - bracketStart - 2);
                if (!int.TryParse(indexStr, out arrayIndex))
                {
                    return (null, null, null, null, $"ERROR: Invalid array index '{indexStr}' in '{part}'");
                }
            }

            // フィールドまたはプロパティを取得
            FieldInfo field = currentType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo property = null;
            object nextObject = null;
            Type nextType = null;

            if (field != null)
            {
                nextType = field.FieldType;
                if (!isLastPart || arrayIndex >= 0)
                {
                    nextObject = field.GetValue(currentObject);
                }
            }
            else
            {
                property = currentType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null)
                {
                    return (null, null, null, null, $"ERROR: Field or property '{fieldName}' not found on '{currentType.Name}'");
                }
                nextType = property.PropertyType;
                if (!isLastPart || arrayIndex >= 0)
                {
                    if (!property.CanRead)
                    {
                        return (null, null, null, null, $"ERROR: Property '{fieldName}' is write-only");
                    }
                    nextObject = property.GetValue(currentObject);
                }
            }

            // 配列/リストインデックスアクセスを処理
            if (arrayIndex >= 0)
            {
                if (nextObject == null)
                {
                    return (null, null, null, null, $"ERROR: Field '{fieldName}' is null, cannot access index [{arrayIndex}]");
                }

                // 配列の場合
                if (nextType.IsArray)
                {
                    Array array = (Array)nextObject;
                    if (arrayIndex < 0 || arrayIndex >= array.Length)
                    {
                        return (null, null, null, null, $"ERROR: Array index {arrayIndex} out of range (length: {array.Length})");
                    }

                    if (isLastPart)
                    {
                        // 最後の要素の場合、配列要素自体を設定対象とする
                        // 特殊処理：配列要素への直接設定
                        return (array, null, null, nextType.GetElementType(), null);
                    }
                    else
                    {
                        nextObject = array.GetValue(arrayIndex);
                        // 実際のランタイム型を使用（重要：基底クラスではなく派生クラスの型）
                        nextType = nextObject != null ? nextObject.GetType() : nextType.GetElementType();
                    }
                }
                // Listの場合
                else if (nextType.IsGenericType && nextType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                {
                    var list = nextObject as System.Collections.IList;
                    if (arrayIndex < 0 || arrayIndex >= list.Count)
                    {
                        return (null, null, null, null, $"ERROR: List index {arrayIndex} out of range (count: {list.Count})");
                    }

                    if (isLastPart)
                    {
                        // 最後の要素の場合、リスト要素自体を設定対象とする
                        return (list, null, null, nextType.GetGenericArguments()[0], null);
                    }
                    else
                    {
                        nextObject = list[arrayIndex];
                        // 実際のランタイム型を使用（重要：基底クラスではなく派生クラスの型）
                        nextType = nextObject != null ? nextObject.GetType() : nextType.GetGenericArguments()[0];
                    }
                }
                else
                {
                    return (null, null, null, null, $"ERROR: Field '{fieldName}' is not an array or list");
                }
            }

            // 最後の要素の場合、フィールド/プロパティ情報を返す
            if (isLastPart && arrayIndex < 0)
            {
                return (currentObject, field, property, nextType, null);
            }

            // 次の階層へ
            if (nextObject == null)
            {
                return (null, null, null, null, $"ERROR: Field '{fieldName}' is null, cannot access nested path");
            }

            currentObject = nextObject;
            currentType = nextType;
        }

        return (null, null, null, null, "ERROR: Unexpected end of path resolution");
    }

    /// <summary>
    /// Parse string value to appropriate type
    /// </summary>
    private object ParseValue(Type targetType, string value)
    {
        try
        {
            // Null or empty
            if (string.IsNullOrEmpty(value))
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            // Bool
            if (targetType == typeof(bool))
            {
                if (value.ToLower() == "true" || value == "1") return true;
                if (value.ToLower() == "false" || value == "0") return false;
                return bool.Parse(value);
            }

            // Int
            if (targetType == typeof(int))
                return int.Parse(value);

            // Float
            if (targetType == typeof(float))
                return float.Parse(value);

            // Double
            if (targetType == typeof(double))
                return double.Parse(value);

            // Long
            if (targetType == typeof(long))
                return long.Parse(value);

            // String
            if (targetType == typeof(string))
                return value;

            // Enum
            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, true);

            // Color (hex format: #RRGGBB or #RRGGBBAA)
            if (targetType == typeof(Color))
            {
                Color color;
                if (ColorUtility.TryParseHtmlString(value, out color))
                    return color;
                return Color.white;
            }

            // Vector2 (format: "x,y")
            if (targetType == typeof(Vector2))
            {
                string[] parts = value.Split(',');
                if (parts.Length == 2)
                    return new Vector2(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()));
            }

            // Vector3 (format: "x,y,z")
            if (targetType == typeof(Vector3))
            {
                string[] parts = value.Split(',');
                if (parts.Length == 3)
                    return new Vector3(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()), float.Parse(parts[2].Trim()));
            }

            // Vector4 (format: "x,y,z,w")
            if (targetType == typeof(Vector4))
            {
                string[] parts = value.Split(',');
                if (parts.Length == 4)
                    return new Vector4(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()), float.Parse(parts[2].Trim()), float.Parse(parts[3].Trim()));
            }

            // Quaternion (format: "x,y,z,w")
            if (targetType == typeof(Quaternion))
            {
                string[] parts = value.Split(',');
                if (parts.Length == 4)
                    return new Quaternion(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()), float.Parse(parts[2].Trim()), float.Parse(parts[3].Trim()));
            }

            // Rect (format: "x,y,width,height")
            if (targetType == typeof(Rect))
            {
                string[] parts = value.Split(',');
                if (parts.Length == 4)
                    return new Rect(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()), float.Parse(parts[2].Trim()), float.Parse(parts[3].Trim()));
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    [McpServerTool, Description("Set GameObject active state (enable/disable)")]
    public async ValueTask<string> SetGameObjectActive(
        [Description("Target GameObject name")] string objectName,
        [Description("Active state (true = enabled, false = disabled)")] string active)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            bool activeState = active.ToLower() == "true" || active == "1";
            targetObject.SetActive(activeState);

            // Mark scene dirty and save
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved after setting {objectName}.SetActive({activeState})");

            return $"SUCCESS: Set {objectName}.SetActive({activeState})";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set active state: {e.Message}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: SetActive only available in Unity Editor";
#endif
    }

    [McpServerTool, Description("Set GameObject tag (Inspector operation)")]
    public async ValueTask<string> SetGameObjectTag(
        [Description("Target GameObject name")] string objectName,
        [Description("Tag name (e.g., 'Player', 'Enemy', 'Untagged')")] string tag)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            // Validate tag exists
            try
            {
                targetObject.tag = tag;
            }
            catch (UnityException)
            {
                return $"ERROR: Tag '{tag}' is not defined in Tag Manager";
            }

            // Mark scene dirty and save
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved after setting {objectName}.tag = '{tag}'");

            return $"SUCCESS: Set {objectName}.tag = '{tag}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set tag: {e.Message}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: SetGameObjectTag only available in Unity Editor";
#endif
    }

    [McpServerTool, Description("List all public fields and properties on a component")]
    public async ValueTask<string> ListComponentFields(
        [Description("Target GameObject name")] string objectName,
        [Description("Component type name")] string componentTypeName)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            var componentType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == componentTypeName && typeof(UnityEngine.Component).IsAssignableFrom(t));

            if (componentType == null)
            {
                return $"ERROR: Component type '{componentTypeName}' not found";
            }

            var component = targetObject.GetComponent(componentType);
            if (component == null)
            {
                return $"ERROR: Component '{componentTypeName}' not found on '{objectName}'";
            }

            string result = $"Public fields and properties on {componentTypeName}:\n\n";

            // Get public fields
            var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            if (fields.Length > 0)
            {
                result += "FIELDS:\n";
                foreach (var field in fields)
                {
                    var currentValue = field.GetValue(component);
                    string valueStr = FormatValue(currentValue);
                    result += $"  {field.Name} ({field.FieldType.Name}): {valueStr}\n";
                }
                result += "\n";
            }

            // Get public properties
            var properties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (properties.Length > 0)
            {
                result += "PROPERTIES:\n";
                foreach (var property in properties)
                {
                    // Skip if property cannot be read
                    if (!property.CanRead)
                    {
                        result += $"  {property.Name} ({property.PropertyType.Name}): [write-only]\n";
                        continue;
                    }

                    try
                    {
                        var currentValue = property.GetValue(component);
                        string valueStr = FormatValue(currentValue);
                        string accessInfo = property.CanWrite ? "" : " [read-only]";
                        result += $"  {property.Name} ({property.PropertyType.Name}): {valueStr}{accessInfo}\n";
                    }
                    catch (Exception ex)
                    {
                        result += $"  {property.Name} ({property.PropertyType.Name}): [error: {ex.Message}]\n";
                    }
                }
            }

            return result.Trim();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to list fields and properties: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }

    /// <summary>
    /// Format value for display
    /// </summary>
    private string FormatValue(object value)
    {
        if (value == null)
            return "null";

        // Vector2
        if (value is Vector2 v2)
            return $"({v2.x}, {v2.y})";

        // Vector3
        if (value is Vector3 v3)
            return $"({v3.x}, {v3.y}, {v3.z})";

        // Vector4
        if (value is Vector4 v4)
            return $"({v4.x}, {v4.y}, {v4.z}, {v4.w})";

        // Quaternion
        if (value is Quaternion q)
            return $"({q.x}, {q.y}, {q.z}, {q.w})";

        // Color
        if (value is Color c)
            return $"#{ColorUtility.ToHtmlStringRGBA(c)}";

        // Rect
        if (value is Rect r)
            return $"(x:{r.x}, y:{r.y}, w:{r.width}, h:{r.height})";

        return value.ToString();
    }

    [McpServerTool, Description("Set array size for any array field (creates new array with default values, supports custom classes)")]
    public async ValueTask<string> SetArraySize(
        [Description("Target GameObject name")] string objectName,
        [Description("Component type name")] string componentTypeName,
        [Description("Array field name")] string fieldName,
        [Description("New array size")] int size)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            var componentType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == componentTypeName && typeof(UnityEngine.Component).IsAssignableFrom(t));

            if (componentType == null)
            {
                return $"ERROR: Component type '{componentTypeName}' not found";
            }

            var component = targetObject.GetComponent(componentType);
            if (component == null)
            {
                return $"ERROR: Component '{componentTypeName}' not found on '{objectName}'";
            }

            FieldInfo field = componentType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                return $"ERROR: Field '{fieldName}' not found on '{componentTypeName}'";
            }

            if (!field.FieldType.IsArray)
            {
                return $"ERROR: Field '{fieldName}' is not an array type";
            }

            Type elementType = field.FieldType.GetElementType();
            Array newArray = Array.CreateInstance(elementType, size);

            // カスタムクラス（非プリミティブ、非Unity Object）の場合、各要素を初期化
            if (!elementType.IsPrimitive && elementType != typeof(string) &&
                !typeof(UnityEngine.Object).IsAssignableFrom(elementType))
            {
                for (int i = 0; i < size; i++)
                {
                    newArray.SetValue(Activator.CreateInstance(elementType), i);
                }
            }

            field.SetValue(component, newArray);

            // Mark scene dirty and save
            UnityEditor.EditorUtility.SetDirty(component);
            UnityEditor.EditorUtility.SetDirty(targetObject);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved after setting {componentTypeName}.{fieldName}.Length = {size}");

            return $"SUCCESS: Set {componentTypeName}.{fieldName} array size to {size}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set array size: {e.Message}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: SetArraySize only available in Unity Editor";
#endif
    }

    [McpServerTool, Description("Set field value on an array element (supports nested custom classes, GameObject references, primitives)")]
    public async ValueTask<string> SetArrayElementField(
        [Description("Target GameObject name")] string objectName,
        [Description("Component type name")] string componentTypeName,
        [Description("Array field name")] string arrayFieldName,
        [Description("Array index")] int index,
        [Description("Element field name")] string elementFieldName,
        [Description("Value (string for primitives, GameObject name for Transform/GameObject/Component references)")] string value)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            var componentType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == componentTypeName && typeof(UnityEngine.Component).IsAssignableFrom(t));

            if (componentType == null)
            {
                return $"ERROR: Component type '{componentTypeName}' not found";
            }

            var component = targetObject.GetComponent(componentType);
            if (component == null)
            {
                return $"ERROR: Component '{componentTypeName}' not found on '{objectName}'";
            }

            FieldInfo arrayField = componentType.GetField(arrayFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (arrayField == null)
            {
                return $"ERROR: Field '{arrayFieldName}' not found on '{componentTypeName}'";
            }

            if (!arrayField.FieldType.IsArray)
            {
                return $"ERROR: Field '{arrayFieldName}' is not an array type";
            }

            Array array = (Array)arrayField.GetValue(component);
            if (array == null)
            {
                return $"ERROR: Array '{arrayFieldName}' is null. Set array size first using SetArraySize.";
            }

            if (index < 0 || index >= array.Length)
            {
                return $"ERROR: Index {index} out of range (array length: {array.Length})";
            }

            // 配列要素を取得
            object element = array.GetValue(index);
            if (element == null)
            {
                return $"ERROR: Array element at index {index} is null";
            }

            // 要素のフィールドを取得
            Type elementType = element.GetType();
            FieldInfo elementField = elementType.GetField(elementFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (elementField == null)
            {
                return $"ERROR: Field '{elementFieldName}' not found on element type '{elementType.Name}'";
            }

            // 値を設定
            object parsedValue = null;
            Type fieldType = elementField.FieldType;

            // GameObject/Transform/Component参照の場合
            if (fieldType == typeof(Transform))
            {
                GameObject valueObj = GameObject.Find(value);
                if (valueObj == null)
                {
                    return $"ERROR: GameObject '{value}' not found";
                }
                parsedValue = valueObj.transform;
            }
            else if (fieldType == typeof(GameObject))
            {
                GameObject valueObj = GameObject.Find(value);
                if (valueObj == null)
                {
                    return $"ERROR: GameObject '{value}' not found";
                }
                parsedValue = valueObj;
            }
            else if (typeof(UnityEngine.Component).IsAssignableFrom(fieldType))
            {
                GameObject valueObj = GameObject.Find(value);
                if (valueObj == null)
                {
                    return $"ERROR: GameObject '{value}' not found";
                }
                var comp = valueObj.GetComponent(fieldType);
                if (comp == null)
                {
                    return $"ERROR: Component '{fieldType.Name}' not found on '{value}'";
                }
                parsedValue = comp;
            }
            else
            {
                // プリミティブ型・string・enum等の場合
                parsedValue = ParseValue(fieldType, value);
                if (parsedValue == null && !string.IsNullOrEmpty(value))
                {
                    return $"ERROR: Failed to parse '{value}' as {fieldType.Name}";
                }
            }

            elementField.SetValue(element, parsedValue);

            // 配列要素が値型（struct）の場合、変更を配列に書き戻す必要がある
            if (elementType.IsValueType)
            {
                array.SetValue(element, index);
            }

            // Mark scene dirty and save
            UnityEditor.EditorUtility.SetDirty(component);
            UnityEditor.EditorUtility.SetDirty(targetObject);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved after setting {componentTypeName}.{arrayFieldName}[{index}].{elementFieldName} = {value}");

            return $"SUCCESS: Set {componentTypeName}.{arrayFieldName}[{index}].{elementFieldName} = {value}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set array element field: {e.Message}\nStack: {e.StackTrace}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: SetArrayElementField only available in Unity Editor";
#endif
    }

    [McpServerTool, Description("Add persistent listener to UnityEvent (supports methods with 0-2 parameters)")]
    public async ValueTask<string> AddUnityEventListener(
        [Description("Target GameObject name")] string objectName,
        [Description("Component type name")] string componentTypeName,
        [Description("UnityEvent field name (can be nested in array, e.g., 'waypointEvents.Array.data[0].onReached')")] string eventFieldPath,
        [Description("Listener GameObject name (object with the method)")] string listenerObjectName,
        [Description("Listener component type name")] string listenerComponentTypeName,
        [Description("Method name to call")] string methodName,
        [Description("Comma-separated parameter values (optional, supports string/int/float/bool)")] string parameters = "")
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            var componentType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == componentTypeName && typeof(UnityEngine.Component).IsAssignableFrom(t));

            if (componentType == null)
            {
                return $"ERROR: Component type '{componentTypeName}' not found";
            }

            var component = targetObject.GetComponent(componentType);
            if (component == null)
            {
                return $"ERROR: Component '{componentTypeName}' not found on '{objectName}'";
            }

            // リスナーオブジェクト取得
            GameObject listenerObject = GameObject.Find(listenerObjectName);
            if (listenerObject == null)
            {
                return $"ERROR: Listener GameObject '{listenerObjectName}' not found";
            }

            var listenerCompType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == listenerComponentTypeName && typeof(UnityEngine.Component).IsAssignableFrom(t));

            if (listenerCompType == null)
            {
                return $"ERROR: Listener component type '{listenerComponentTypeName}' not found";
            }

            var listenerComponent = listenerObject.GetComponent(listenerCompType);
            if (listenerComponent == null)
            {
                return $"ERROR: Component '{listenerComponentTypeName}' not found on '{listenerObjectName}'";
            }

            // メソッド検証
            var method = listenerCompType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                return $"ERROR: Method '{methodName}' not found on '{listenerComponentTypeName}'";
            }

            // SerializedObjectを使用してUnityEvent設定
            var serializedObject = new UnityEditor.SerializedObject(component);
            var property = serializedObject.FindProperty(eventFieldPath);

            if (property == null)
            {
                return $"ERROR: UnityEvent field '{eventFieldPath}' not found on '{componentTypeName}'";
            }

            // persistentCallsを取得
            var persistentCallsProp = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (persistentCallsProp == null)
            {
                return $"ERROR: '{eventFieldPath}' is not a UnityEvent (missing m_PersistentCalls)";
            }

            // 新しいリスナーを追加
            int newIndex = persistentCallsProp.arraySize;
            persistentCallsProp.InsertArrayElementAtIndex(newIndex);
            var callProp = persistentCallsProp.GetArrayElementAtIndex(newIndex);

            // target設定
            callProp.FindPropertyRelative("m_Target").objectReferenceValue = listenerComponent;
            callProp.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = listenerCompType.AssemblyQualifiedName;
            callProp.FindPropertyRelative("m_MethodName").stringValue = methodName;
            callProp.FindPropertyRelative("m_CallState").enumValueIndex = 2; // RuntimeOnly

            // パラメータ解析
            var paramArray = string.IsNullOrEmpty(parameters) ? new string[0] : parameters.Split(',');
            for (int i = 0; i < paramArray.Length; i++)
            {
                paramArray[i] = paramArray[i].Trim();
            }

            var methodParams = method.GetParameters();

            // mode設定（パラメータ数に基づく）
            if (methodParams.Length == 0)
            {
                callProp.FindPropertyRelative("m_Mode").enumValueIndex = 1; // Void
            }
            else if (methodParams.Length == 1)
            {
                var paramType = methodParams[0].ParameterType;

                if (paramType == typeof(string))
                {
                    callProp.FindPropertyRelative("m_Mode").enumValueIndex = 5; // String
                    if (paramArray.Length > 0)
                        callProp.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue = paramArray[0];
                }
                else if (paramType == typeof(int))
                {
                    callProp.FindPropertyRelative("m_Mode").enumValueIndex = 3; // Int
                    if (paramArray.Length > 0 && int.TryParse(paramArray[0], out int intVal))
                        callProp.FindPropertyRelative("m_Arguments.m_IntArgument").intValue = intVal;
                }
                else if (paramType == typeof(float))
                {
                    callProp.FindPropertyRelative("m_Mode").enumValueIndex = 4; // Float
                    if (paramArray.Length > 0 && float.TryParse(paramArray[0], out float floatVal))
                        callProp.FindPropertyRelative("m_Arguments.m_FloatArgument").floatValue = floatVal;
                }
                else if (paramType == typeof(bool))
                {
                    callProp.FindPropertyRelative("m_Mode").enumValueIndex = 6; // Bool
                    if (paramArray.Length > 0 && bool.TryParse(paramArray[0], out bool boolVal))
                        callProp.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue = boolVal;
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(paramType))
                {
                    callProp.FindPropertyRelative("m_Mode").enumValueIndex = 2; // Object
                    callProp.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue = paramType.AssemblyQualifiedName;
                }
            }
            else if (methodParams.Length == 2)
            {
                // 2パラメータの場合（例：string, string）
                callProp.FindPropertyRelative("m_Mode").enumValueIndex = 5; // String (最初のパラメータ)
                if (paramArray.Length > 0)
                    callProp.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue = paramArray[0];

                // 2番目のパラメータは動的パラメータとして渡される想定
                // UnityEventの制約上、2パラメータは特殊な設定が必要
                // ここでは簡易的に最初のパラメータのみ設定
            }

            serializedObject.ApplyModifiedProperties();

            // Mark scene dirty and save
            UnityEditor.EditorUtility.SetDirty(component);
            UnityEditor.EditorUtility.SetDirty(targetObject);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene '{scene.name}' saved after adding UnityEvent listener: {componentTypeName}.{eventFieldPath} -> {listenerComponentTypeName}.{methodName}");

            return $"SUCCESS: Added listener {listenerComponentTypeName}.{methodName} to {componentTypeName}.{eventFieldPath}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to add UnityEvent listener: {e.Message}\nStack: {e.StackTrace}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: AddUnityEventListener only available in Unity Editor";
#endif
    }

    [McpServerTool, Description("Add Volume Override to Volume Profile (e.g., Vignette, FilmGrain, ChromaticAberration)")]
    public async ValueTask<string> AddVolumeOverride(
        [Description("Target GameObject name with Volume component")] string objectName,
        [Description("Volume Override type name (e.g., Vignette, FilmGrain, ChromaticAberration, LensDistortion)")] string overrideTypeName)
    {
#if UNITY_EDITOR
        try
        {
            await UniTask.SwitchToMainThread();

            // Find target GameObject
            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return $"ERROR: GameObject '{objectName}' not found";
            }

            // Get Volume component via reflection
            var volumeType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "Volume" && typeof(UnityEngine.Component).IsAssignableFrom(t));

            if (volumeType == null)
            {
                return $"ERROR: Volume type not found";
            }

            var volume = targetObject.GetComponent(volumeType);
            if (volume == null)
            {
                return $"ERROR: Volume component not found on '{objectName}'";
            }

            // Get sharedProfile via reflection (it's a field, not property)
            var sharedProfileField = volumeType.GetField("sharedProfile", BindingFlags.Public | BindingFlags.Instance);
            if (sharedProfileField == null)
            {
                return $"ERROR: sharedProfile field not found on Volume";
            }

            var profile = sharedProfileField.GetValue(volume);
            if (profile == null)
            {
                return $"ERROR: Volume Profile not found on '{objectName}'";
            }

            // Cast profile to UnityEngine.Object for later use
            var profileObj = profile as UnityEngine.Object;
            if (profileObj == null)
            {
                return $"ERROR: Volume Profile is not a UnityEngine.Object";
            }

            // Find VolumeComponent base type
            var volumeComponentBaseType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "VolumeComponent");

            if (volumeComponentBaseType == null)
            {
                return $"ERROR: VolumeComponent base type not found";
            }

            // Find VolumeComponent type
            var overrideType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == overrideTypeName && t.IsSubclassOf(volumeComponentBaseType));

            if (overrideType == null)
            {
                return $"ERROR: VolumeComponent type '{overrideTypeName}' not found";
            }

            // Get components list via reflection
            var componentsField = profile.GetType().GetField("components", BindingFlags.Public | BindingFlags.Instance);
            if (componentsField == null)
            {
                return $"ERROR: components field not found on Volume Profile";
            }

            var componentsList = componentsField.GetValue(profile) as System.Collections.IList;
            if (componentsList == null)
            {
                return $"ERROR: components list is null";
            }

            // Check if already exists
            foreach (var existingComponent in componentsList)
            {
                if (existingComponent.GetType() == overrideType)
                {
                    return $"INFO: '{overrideTypeName}' already exists in Volume Profile";
                }
            }

            // Create new VolumeComponent instance
            var newComponent = ScriptableObject.CreateInstance(overrideType);
            if (newComponent == null)
            {
                return $"ERROR: Failed to create instance of '{overrideTypeName}'";
            }

            newComponent.name = overrideTypeName;

            // Add to profile's components list
            componentsList.Add(newComponent);

            // Add as sub-asset to the profile asset
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(profileObj);
            UnityEditor.AssetDatabase.AddObjectToAsset(newComponent, assetPath);

            // Mark dirty and save
            UnityEditor.EditorUtility.SetDirty(newComponent);
            UnityEditor.EditorUtility.SetDirty(profileObj);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"Added '{overrideTypeName}' to Volume Profile: {profileObj.name}");

            return $"SUCCESS: Added '{overrideTypeName}' to Volume Profile on '{objectName}'";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to add Volume Override: {e.Message}\nStack: {e.StackTrace}");
            return $"ERROR: {e.Message}";
        }
#else
        await UniTask.Yield();
        return "ERROR: AddVolumeOverride only available in Unity Editor";
#endif
    }
}
