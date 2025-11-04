using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public class BoneTransformAnalyzer : EditorWindow
{
    private string koiModelPath = "Assets/JSFreshwaterfish/Koi/Models/Anim_Koi.FBX";
    private string carpModelPath = "Assets/JapaneseRiverFishPack/fbx/carp.fbx";

    [MenuItem("Tools/Analyze Bone Transforms")]
    static void Init()
    {
        BoneTransformAnalyzer window = (BoneTransformAnalyzer)EditorWindow.GetWindow(typeof(BoneTransformAnalyzer));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Bone Transform Analyzer", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        if (GUILayout.Button("Analyze Both Models"))
        {
            AnalyzeBothModels();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Analyze Koi Model"))
        {
            AnalyzeModel(koiModelPath, new string[] { "Spine.1", "Spine.2", "Spine.3", "Spine.4", "Spine.5" }, "Koi");
        }

        if (GUILayout.Button("Analyze Carp Model"))
        {
            AnalyzeModel(carpModelPath, new string[] { "Body1", "Body2", "Body3", "Body4", "Body5" }, "Carp");
        }
    }

    void AnalyzeBothModels()
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine("================================================================================");
        report.AppendLine("BONE TRANSFORM ANALYSIS REPORT");
        report.AppendLine("================================================================================");
        report.AppendLine();

        // Analyze Koi
        report.AppendLine(AnalyzeModelInternal(koiModelPath, new string[] { "Spine.1", "Spine.2", "Spine.3", "Spine.4", "Spine.5" }, "Koi"));

        report.AppendLine();
        report.AppendLine();

        // Analyze Carp
        report.AppendLine(AnalyzeModelInternal(carpModelPath, new string[] { "Body1", "Body2", "Body3", "Body4", "Body5" }, "Carp"));

        // Save report
        string reportPath = "Assets/BoneTransformAnalysisReport.txt";
        File.WriteAllText(reportPath, report.ToString());

        Debug.Log("Analysis complete! Report saved to: " + reportPath);
        Debug.Log(report.ToString());

        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(reportPath);
    }

    void AnalyzeModel(string modelPath, string[] boneNames, string modelName)
    {
        string result = AnalyzeModelInternal(modelPath, boneNames, modelName);
        Debug.Log(result);
    }

    string AnalyzeModelInternal(string modelPath, string[] boneNames, string modelName)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("================================================================================");
        sb.AppendLine($"{modelName.ToUpper()} MODEL ANALYSIS");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"Model Path: {modelPath}");
        sb.AppendLine();

        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

        if (modelPrefab == null)
        {
            sb.AppendLine($"ERROR: Could not load model at path: {modelPath}");
            return sb.ToString();
        }

        // Analyze model import settings
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer != null)
        {
            sb.AppendLine("Import Settings:");
            sb.AppendLine($"  Global Scale: {importer.globalScale}");
            sb.AppendLine($"  Use File Scale: {importer.useFileScale}");
            sb.AppendLine($"  File Scale: {importer.fileScale}");
            sb.AppendLine($"  Animation Type: {importer.animationType}");
            sb.AppendLine();
        }

        // Find all transforms in the model
        Transform[] allTransforms = modelPrefab.GetComponentsInChildren<Transform>(true);

        sb.AppendLine($"Total Transforms in model: {allTransforms.Length}");
        sb.AppendLine();

        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine("BONE TRANSFORM DETAILS");
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine();

        foreach (string boneName in boneNames)
        {
            Transform bone = FindTransformByName(allTransforms, boneName);

            if (bone != null)
            {
                sb.AppendLine($"Bone: {boneName}");
                sb.AppendLine($"  Full Path: {GetFullPath(bone)}");
                sb.AppendLine();

                // Local Transform
                sb.AppendLine("  Local Transform:");
                sb.AppendLine($"    Position: {FormatVector3(bone.localPosition)}");
                sb.AppendLine($"    Rotation: {FormatQuaternion(bone.localRotation)}");
                sb.AppendLine($"    Euler Angles: {FormatVector3(bone.localEulerAngles)}");
                sb.AppendLine($"    Scale: {FormatVector3(bone.localScale)}");
                sb.AppendLine();

                // World Transform
                sb.AppendLine("  World Transform:");
                sb.AppendLine($"    Position: {FormatVector3(bone.position)}");
                sb.AppendLine($"    Rotation: {FormatQuaternion(bone.rotation)}");
                sb.AppendLine($"    Euler Angles: {FormatVector3(bone.eulerAngles)}");
                sb.AppendLine();

                // Determine forward direction
                Vector3 localForward = bone.localRotation * Vector3.forward;
                Vector3 localUp = bone.localRotation * Vector3.up;
                Vector3 localRight = bone.localRotation * Vector3.right;

                sb.AppendLine("  Local Axis Directions (in parent space):");
                sb.AppendLine($"    Forward: {FormatVector3(localForward)}");
                sb.AppendLine($"    Up: {FormatVector3(localUp)}");
                sb.AppendLine($"    Right: {FormatVector3(localRight)}");
                sb.AppendLine();

                // Determine primary axis
                string primaryAxis = DeterminePrimaryAxis(localForward, localUp, localRight);
                sb.AppendLine($"  Bone Forward Axis: {primaryAxis}");
                sb.AppendLine();

                // Parent info
                if (bone.parent != null)
                {
                    sb.AppendLine($"  Parent: {bone.parent.name}");
                    sb.AppendLine($"    Parent Rotation: {FormatQuaternion(bone.parent.localRotation)}");
                    sb.AppendLine($"    Parent Euler: {FormatVector3(bone.parent.localEulerAngles)}");
                }
                else
                {
                    sb.AppendLine("  Parent: None (Root)");
                }

                sb.AppendLine();
                sb.AppendLine("  ----------------------------------------------------------------");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"Bone: {boneName}");
                sb.AppendLine("  ERROR: Bone not found in model!");
                sb.AppendLine();
            }
        }

        // Coordinate System Analysis
        sb.AppendLine("================================================================================");
        sb.AppendLine("COORDINATE SYSTEM ANALYSIS");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        // Check root transform orientation
        Transform root = modelPrefab.transform;
        sb.AppendLine($"Root Object: {root.name}");
        sb.AppendLine($"  Local Rotation: {FormatQuaternion(root.localRotation)}");
        sb.AppendLine($"  Local Euler: {FormatVector3(root.localEulerAngles)}");
        sb.AppendLine();

        // Try to determine coordinate system from bone chain
        AnalyzeCoordinateSystem(allTransforms, boneNames, sb);

        return sb.ToString();
    }

    Transform FindTransformByName(Transform[] transforms, string name)
    {
        foreach (Transform t in transforms)
        {
            if (t.name == name)
                return t;
        }
        return null;
    }

    string GetFullPath(Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    string FormatVector3(Vector3 v)
    {
        return $"({v.x:F6}, {v.y:F6}, {v.z:F6})";
    }

    string FormatQuaternion(Quaternion q)
    {
        return $"({q.x:F6}, {q.y:F6}, {q.z:F6}, {q.w:F6})";
    }

    string DeterminePrimaryAxis(Vector3 forward, Vector3 up, Vector3 right)
    {
        // Determine which axis the bone is primarily aligned with
        float fwdX = Mathf.Abs(forward.x);
        float fwdY = Mathf.Abs(forward.y);
        float fwdZ = Mathf.Abs(forward.z);

        if (fwdX > fwdY && fwdX > fwdZ)
        {
            return forward.x > 0 ? "+X (Right)" : "-X (Left)";
        }
        else if (fwdY > fwdX && fwdY > fwdZ)
        {
            return forward.y > 0 ? "+Y (Up)" : "-Y (Down)";
        }
        else
        {
            return forward.z > 0 ? "+Z (Forward)" : "-Z (Back)";
        }
    }

    void AnalyzeCoordinateSystem(Transform[] allTransforms, string[] boneNames, StringBuilder sb)
    {
        // Find bone chain positions to determine orientation
        Transform firstBone = FindTransformByName(allTransforms, boneNames[0]);
        Transform lastBone = FindTransformByName(allTransforms, boneNames[boneNames.Length - 1]);

        if (firstBone != null && lastBone != null)
        {
            Vector3 chainDirection = (lastBone.position - firstBone.position).normalized;

            sb.AppendLine($"Bone Chain Analysis:");
            sb.AppendLine($"  First Bone: {boneNames[0]} at {FormatVector3(firstBone.position)}");
            sb.AppendLine($"  Last Bone: {boneNames[boneNames.Length - 1]} at {FormatVector3(lastBone.position)}");
            sb.AppendLine($"  Chain Direction: {FormatVector3(chainDirection)}");
            sb.AppendLine();

            float x = Mathf.Abs(chainDirection.x);
            float y = Mathf.Abs(chainDirection.y);
            float z = Mathf.Abs(chainDirection.z);

            string primaryChainAxis;
            if (x > y && x > z)
                primaryChainAxis = chainDirection.x > 0 ? "+X" : "-X";
            else if (y > x && y > z)
                primaryChainAxis = chainDirection.y > 0 ? "+Y" : "-Y";
            else
                primaryChainAxis = chainDirection.z > 0 ? "+Z" : "-Z";

            sb.AppendLine($"  Primary Chain Axis: {primaryChainAxis}");
            sb.AppendLine($"  Interpretation: The spine extends along the {primaryChainAxis} axis");
            sb.AppendLine();
        }
    }
}
