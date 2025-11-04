using UnityEngine;
using UnityEditor;

/// <summary>
/// 実際のプレハブからボーンの初期回転を抽出するツール
/// </summary>
public class BoneRotationAnalyzer : EditorWindow
{
    private GameObject koiPrefab;
    private GameObject carpPrefab;

    [MenuItem("Tools/Bone Rotation Analyzer")]
    public static void ShowWindow()
    {
        GetWindow<BoneRotationAnalyzer>("Bone Rotation Analyzer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ボーン初期姿勢解析ツール", EditorStyles.boldLabel);

        koiPrefab = (GameObject)EditorGUILayout.ObjectField("Koiプレハブ", koiPrefab, typeof(GameObject), false);
        carpPrefab = (GameObject)EditorGUILayout.ObjectField("Carpプレハブ", carpPrefab, typeof(GameObject), false);

        if (GUILayout.Button("ボーン姿勢を解析"))
        {
            AnalyzeBones();
        }
    }

    private void AnalyzeBones()
    {
        if (koiPrefab == null || carpPrefab == null)
        {
            EditorUtility.DisplayDialog("エラー", "両方のプレハブを設定してください", "OK");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("========== ボーン姿勢解析 ==========");

        // Koiプレハブをインスタンス化
        GameObject koiInstance = PrefabUtility.InstantiatePrefab(koiPrefab) as GameObject;

        sb.AppendLine("\n===== Koiモデル =====");
        // 階層を探索して正しいパスを見つける
        FindAndAnalyze(koiInstance.transform, "Spine.1", sb);
        FindAndAnalyze(koiInstance.transform, "Spine.2", sb);
        FindAndAnalyze(koiInstance.transform, "Spine.3", sb);
        FindAndAnalyze(koiInstance.transform, "Spine.4", sb);
        FindAndAnalyze(koiInstance.transform, "Spine.5", sb);

        DestroyImmediate(koiInstance);

        // Carpプレハブをインスタンス化
        GameObject carpInstance = PrefabUtility.InstantiatePrefab(carpPrefab) as GameObject;

        sb.AppendLine("\n===== Carpモデル =====");
        FindAndAnalyze(carpInstance.transform, "Body1", sb);
        FindAndAnalyze(carpInstance.transform, "Body2", sb);
        FindAndAnalyze(carpInstance.transform, "Body3", sb);
        FindAndAnalyze(carpInstance.transform, "Body4", sb);
        FindAndAnalyze(carpInstance.transform, "Body5", sb);

        DestroyImmediate(carpInstance);

        sb.AppendLine("\n========== 解析完了 ==========");

        // 一括出力
        Debug.Log(sb.ToString());
    }

    private void FindAndAnalyze(Transform root, string boneName, System.Text.StringBuilder sb)
    {
        Transform bone = FindBoneRecursive(root, boneName);
        if (bone == null)
        {
            sb.AppendLine($"ボーンが見つかりません: {boneName}");
            return;
        }

        AnalyzeTransform(bone, GetPath(bone), sb);
    }

    private Transform FindBoneRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindBoneRecursive(child, name);
            if (result != null) return result;
        }

        return null;
    }

    private string GetPath(Transform bone)
    {
        string path = bone.name;
        Transform current = bone.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    private void AnalyzeTransform(Transform bone, string path, System.Text.StringBuilder sb)
    {

        Quaternion localRot = bone.localRotation;
        Vector3 euler = bone.localRotation.eulerAngles;
        Vector3 forward = bone.forward;
        Vector3 up = bone.up;
        Vector3 right = bone.right;

        sb.AppendLine($"\n--- {path} ---");
        sb.AppendLine($"  LocalRotation (Quat): ({localRot.x:F4}, {localRot.y:F4}, {localRot.z:F4}, {localRot.w:F4})");
        sb.AppendLine($"  LocalRotation (Euler): ({euler.x:F2}, {euler.y:F2}, {euler.z:F2})");
        sb.AppendLine($"  Forward: ({forward.x:F4}, {forward.y:F4}, {forward.z:F4})");
        sb.AppendLine($"  Up: ({up.x:F4}, {up.y:F4}, {up.z:F4})");
        sb.AppendLine($"  Right: ({right.x:F4}, {right.y:F4}, {right.z:F4})");
    }
}
