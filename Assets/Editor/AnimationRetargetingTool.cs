using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// アニメーションクリップのボーン参照を別のモデル用にリターゲットするエディタツール
/// 元のアニメーションは保護し、必ず複製してから変換を行う
/// </summary>
public class AnimationRetargetingTool : EditorWindow
{
    [Header("ソース設定")]
    [Tooltip("変換元のアニメーションクリップ")]
    private AnimationClip sourceClip;

    [Tooltip("ソースモデルのルートTransform")]
    private Transform sourceModelRoot;

    [Header("ターゲット設定")]
    [Tooltip("ターゲットモデルのルートTransform")]
    private Transform targetModelRoot;

    [Tooltip("変換後のアニメーションの保存先フォルダ")]
    private string outputFolder = "Assets/Animations/Retargeted";

    [Header("ボーンマッピング（自動検出）")]
    private Dictionary<string, string> boneMapping = new Dictionary<string, string>();

    [Header("プレビュー")]
    private Vector2 scrollPosition;
    private bool showMapping = true;

    [Header("回転補正")]
    private bool enableRotationFix = false; // デフォルトOFFに変更

    // Koi→Carp用の回転補正（実測値から計算）
    // Koi Spine.1初期姿勢: (0, -0.7028, 0.7113, 0)
    // Carp Body1初期姿勢: (-0.7071, 0, 0, 0.7071)
    private static readonly Quaternion KoiSpine1RestPose = new Quaternion(0f, -0.7028f, 0.7113f, 0f);
    private static readonly Quaternion CarpBody1RestPose = new Quaternion(-0.7071f, 0f, 0f, 0.7071f);
    private static readonly Quaternion KoiToCarpRotationOffset = Quaternion.Inverse(KoiSpine1RestPose) * CarpBody1RestPose;

    // 鯉用のデフォルトマッピング
    private static readonly Dictionary<string, string> DefaultKoiToCarpMapping = new Dictionary<string, string>()
    {
        // 体幹部分（最重要）
        { "Spine.1", "Body1" },
        { "Spine.2", "Body2" },
        { "Spine.3", "Body3" },
        { "Spine.4", "Body4" },
        { "Spine.5", "Body5" },

        // 尾びれ（近似マッピング）
        { "CAudalFin.1.1", "Tail1_1" },
        { "CAudalFin.1.2", "Tail1_2" },
        { "CAudalFin.2.1", "Tail2_1" },
        { "CAudalFin.2.2", "Tail2_2" },

        // 頭部
        { "Head", "Head" },
        { "UpperLip", "UpperMouth" },
        { "LowerMouth", "LowerMouth" },

        // ヒレ（対応するもののみ）
        // 胸ビレ - KoiのPectralFinはCarpのPectoralFinに対応
        { "PectralFin1.1_L", "PectoralFin1_L" },
        { "PectralFin1.1_R", "PectoralFin1_R" },

        // 腹ビレ - KoiのPelvicFinはCarpのVentralFinに対応
        { "PelvicFin1_L", "VentralFin1_L" },
        { "PelvicFin1_R", "VentralFin1_R" },

        // 背びれ（部分的にマッピング）
        { "DorsalFin.1", "DorsalFin1" },
        { "DorsalFin.3", "DorsalFin2" },
        { "DorsalFin.4", "DorsalFin3" },

        // 臀びれ
        { "AnalFin.1.1", "AnalFin1" },
        { "AnalFin.2", "AnalFin2" },
    };

    [MenuItem("Tools/Animation Retargeting Tool")]
    public static void ShowWindow()
    {
        GetWindow<AnimationRetargetingTool>("Animation Retargeting");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("アニメーションリターゲティングツール", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("元のアニメーションは保護され、必ず複製してから変換が行われます。", MessageType.Info);

        EditorGUILayout.Space();

        // ソース設定
        EditorGUILayout.LabelField("ソース設定", EditorStyles.boldLabel);
        sourceClip = (AnimationClip)EditorGUILayout.ObjectField("変換元アニメーション", sourceClip, typeof(AnimationClip), false);
        sourceModelRoot = (Transform)EditorGUILayout.ObjectField("ソースモデルRoot", sourceModelRoot, typeof(Transform), true);

        EditorGUILayout.Space();

        // ターゲット設定
        EditorGUILayout.LabelField("ターゲット設定", EditorStyles.boldLabel);
        targetModelRoot = (Transform)EditorGUILayout.ObjectField("ターゲットモデルRoot", targetModelRoot, typeof(Transform), true);
        outputFolder = EditorGUILayout.TextField("出力フォルダ", outputFolder);

        EditorGUILayout.Space();

        // ボーンマッピング設定
        showMapping = EditorGUILayout.Foldout(showMapping, "ボーンマッピング設定");
        if (showMapping)
        {
            EditorGUILayout.HelpBox("デフォルトでKoi→Carpのマッピングが適用されます", MessageType.Info);

            if (GUILayout.Button("デフォルトマッピング（Koi→Carp）を読み込み"))
            {
                LoadDefaultMapping();
            }

            if (GUILayout.Button("自動検出（名前が一致するボーンのみ）"))
            {
                AutoDetectMapping();
            }

            EditorGUILayout.Space();

            // マッピングテーブル表示
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            EditorGUILayout.LabelField("ソースボーン → ターゲットボーン", EditorStyles.boldLabel);

            if (boneMapping.Count == 0)
            {
                EditorGUILayout.HelpBox("マッピングがありません。デフォルトマッピングを読み込むか、自動検出を実行してください。", MessageType.Warning);
            }
            else
            {
                var keys = boneMapping.Keys.ToList();
                foreach (var sourceKey in keys)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(sourceKey, GUILayout.Width(150));
                    EditorGUILayout.LabelField("→", GUILayout.Width(20));
                    boneMapping[sourceKey] = EditorGUILayout.TextField(boneMapping[sourceKey], GUILayout.Width(150));

                    if (GUILayout.Button("削除", GUILayout.Width(50)))
                    {
                        boneMapping.Remove(sourceKey);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space();

        // 回転補正設定
        EditorGUILayout.LabelField("回転補正", EditorStyles.boldLabel);
        enableRotationFix = EditorGUILayout.Toggle("回転補正を有効化（Koi→Carp）", enableRotationFix);
        if (enableRotationFix)
        {
            EditorGUILayout.HelpBox("KoiとCarpのボーン座標系の違いを自動補正します（Z軸180度回転）", MessageType.Info);
        }

        EditorGUILayout.Space();

        // 変換実行ボタン
        GUI.enabled = sourceClip != null && targetModelRoot != null && boneMapping.Count > 0;

        if (GUILayout.Button("アニメーションをリターゲット（複製して変換）", GUILayout.Height(40)))
        {
            RetargetAnimation();
        }

        GUI.enabled = true;
    }

    private void LoadDefaultMapping()
    {
        boneMapping = new Dictionary<string, string>(DefaultKoiToCarpMapping);
        Debug.Log($"デフォルトマッピングを読み込みました: {boneMapping.Count}個のボーン対応");
    }

    private void AutoDetectMapping()
    {
        if (sourceModelRoot == null || targetModelRoot == null)
        {
            EditorUtility.DisplayDialog("エラー", "ソースモデルとターゲットモデルの両方を設定してください", "OK");
            return;
        }

        boneMapping.Clear();

        // ソースモデルの全ボーンを取得
        Transform[] sourceBones = sourceModelRoot.GetComponentsInChildren<Transform>();
        Transform[] targetBones = targetModelRoot.GetComponentsInChildren<Transform>();

        // 名前が完全一致するボーンをマッピング
        foreach (var sourceBone in sourceBones)
        {
            foreach (var targetBone in targetBones)
            {
                if (sourceBone.name == targetBone.name)
                {
                    boneMapping[sourceBone.name] = targetBone.name;
                    break;
                }
            }
        }

        Debug.Log($"自動検出完了: {boneMapping.Count}個のボーンが一致しました");
    }

    private void RetargetAnimation()
    {
        // 出力フォルダの確認・作成
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            string[] folders = outputFolder.Split('/');
            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }

        // アニメーションクリップを新規作成（元を保護）
        AnimationClip newClip = new AnimationClip();
        newClip.name = sourceClip.name + "_Retargeted";
        newClip.frameRate = sourceClip.frameRate;
        newClip.wrapMode = sourceClip.wrapMode;
        newClip.legacy = sourceClip.legacy;

        // アニメーションイベントもコピー
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(sourceClip);
        AnimationUtility.SetAnimationEvents(newClip, events);

        // ボーン参照を書き換え
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);

        int successCount = 0;
        int skipCount = 0;

        foreach (EditorCurveBinding binding in bindings)
        {
            string originalPath = binding.path;
            string newPath = ConvertBonePath(originalPath);

            // 変換できなかった（元のパスと同じ）場合はスキップ
            if (newPath == originalPath)
            {
                skipCount++;
                continue; // このカーブは追加しない
            }

            // カーブを取得
            AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);

            // 回転カーブの場合、補正を適用
            if (enableRotationFix && binding.type == typeof(Transform) &&
                (binding.propertyName.StartsWith("m_LocalRotation")))
            {
                // 回転補正は後でまとめて処理（Quaternionの4成分を同時に扱う必要があるため）
                // ここでは通常通りコピー
            }

            // 新しいパスでカーブを設定
            EditorCurveBinding newBinding = binding;
            newBinding.path = newPath;

            AnimationUtility.SetEditorCurve(newClip, newBinding, curve);
            successCount++;
        }

        // 回転補正を適用
        if (enableRotationFix)
        {
            ApplyRotationFix(newClip, sourceClip);
        }

        // オブジェクトカーブ（Transform以外）も処理
        EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(sourceClip);
        foreach (var binding in objectBindings)
        {
            string newPath = ConvertBonePath(binding.path);

            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);

            EditorCurveBinding newBinding = binding;
            newBinding.path = newPath;

            AnimationUtility.SetObjectReferenceCurve(newClip, newBinding, keyframes);
        }

        // ファイルとして保存
        string savePath = $"{outputFolder}/{newClip.name}.anim";
        AssetDatabase.CreateAsset(newClip, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>リターゲット完了!</color>\n" +
                  $"保存先: {savePath}\n" +
                  $"変換: {successCount}カーブ, スキップ: {skipCount}カーブ");

        EditorUtility.DisplayDialog("完了",
            $"アニメーションのリターゲットが完了しました。\n\n" +
            $"保存先: {savePath}\n" +
            $"変換: {successCount}カーブ\n" +
            $"スキップ: {skipCount}カーブ\n\n" +
            $"元のアニメーションは保護されています。", "OK");

        // 保存したアセットを選択
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(savePath);
    }

    /// <summary>
    /// 回転補正を適用（ワールド回転ベース変換）
    /// </summary>
    private void ApplyRotationFix(AnimationClip targetClip, AnimationClip sourceClip)
    {
        // ソースモデルとターゲットモデルのレストポーズ階層を構築
        Dictionary<string, Quaternion> koiRestPoses = BuildRestPoseHierarchy(sourceModelRoot, "Spine");
        Dictionary<string, Quaternion> carpRestPoses = BuildRestPoseHierarchy(targetModelRoot, "Body");

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(targetClip);

        // 全タイムスタンプを収集
        HashSet<float> allTimes = new HashSet<float>();
        foreach (var binding in bindings)
        {
            if (binding.propertyName == "m_LocalRotation.x")
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(targetClip, binding);
                foreach (var key in curve.keys)
                {
                    allTimes.Add(key.time);
                }
            }
        }

        // 各タイムスタンプでワールド回転を計算
        foreach (float time in allTimes)
        {
            // ソースアニメーションから各ボーンのローカル回転を取得してワールド回転を計算
            Dictionary<string, Quaternion> koiWorldRotations = CalculateWorldRotations(sourceClip, koiRestPoses, time, "Spine");

            // ターゲットのローカル回転に逆変換
            ConvertAndApplyRotations(targetClip, koiWorldRotations, carpRestPoses, time);
        }

        Debug.Log("回転補正を適用しました（ワールド回転ベース変換）");
    }

    /// <summary>
    /// モデルからレストポーズの階層を構築
    /// </summary>
    private Dictionary<string, Quaternion> BuildRestPoseHierarchy(Transform root, string bonePrefix)
    {
        Dictionary<string, Quaternion> restPoses = new Dictionary<string, Quaternion>();

        for (int i = 1; i <= 5; i++)
        {
            string boneName = bonePrefix + (bonePrefix == "Spine" ? "." + i : i.ToString());
            Transform bone = FindBoneRecursive(root, boneName);

            if (bone != null)
            {
                // ワールド回転を保存
                restPoses[boneName] = bone.rotation;
            }
        }

        return restPoses;
    }

    /// <summary>
    /// 再帰的にボーンを検索
    /// </summary>
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

    /// <summary>
    /// アニメーションから指定時刻のワールド回転を計算
    /// </summary>
    private Dictionary<string, Quaternion> CalculateWorldRotations(AnimationClip clip, Dictionary<string, Quaternion> restPoses, float time, string bonePrefix)
    {
        Dictionary<string, Quaternion> worldRotations = new Dictionary<string, Quaternion>();

        // 各ボーンのローカル回転を取得
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

        Quaternion parentWorldRot = Quaternion.identity;

        for (int i = 1; i <= 5; i++)
        {
            string boneName = bonePrefix + (bonePrefix == "Spine" ? "." + i : i.ToString());

            // このボーンのローカル回転カーブを取得
            Quaternion localRot = GetRotationAtTime(clip, bindings, boneName, time);

            // ワールド回転 = 親のワールド回転 * 自分のローカル回転
            Quaternion worldRot = parentWorldRot * localRot;
            worldRotations[boneName] = worldRot;

            // 次のボーンの親として使用
            parentWorldRot = worldRot;
        }

        return worldRotations;
    }

    /// <summary>
    /// 指定時刻のローカル回転を取得
    /// </summary>
    private Quaternion GetRotationAtTime(AnimationClip clip, EditorCurveBinding[] bindings, string boneName, float time)
    {
        // パスからボーンを探す（部分一致）
        var rotBindings = bindings.Where(b => b.path.Contains(boneName) && b.propertyName.StartsWith("m_LocalRotation")).ToArray();

        if (rotBindings.Length < 4) return Quaternion.identity;

        float x = AnimationUtility.GetEditorCurve(clip, rotBindings.First(b => b.propertyName == "m_LocalRotation.x")).Evaluate(time);
        float y = AnimationUtility.GetEditorCurve(clip, rotBindings.First(b => b.propertyName == "m_LocalRotation.y")).Evaluate(time);
        float z = AnimationUtility.GetEditorCurve(clip, rotBindings.First(b => b.propertyName == "m_LocalRotation.z")).Evaluate(time);
        float w = AnimationUtility.GetEditorCurve(clip, rotBindings.First(b => b.propertyName == "m_LocalRotation.w")).Evaluate(time);

        return new Quaternion(x, y, z, w);
    }

    /// <summary>
    /// ワールド回転をターゲットのローカル回転に変換して適用
    /// </summary>
    private void ConvertAndApplyRotations(AnimationClip targetClip, Dictionary<string, Quaternion> sourceWorldRotations, Dictionary<string, Quaternion> targetRestPoses, float time)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(targetClip);

        Quaternion parentWorldRot = Quaternion.identity;

        for (int i = 1; i <= 5; i++)
        {
            string sourceBone = "Spine." + i;
            string targetBone = "Body" + i;
            string targetPath = "Root/" + targetBone;

            if (!sourceWorldRotations.ContainsKey(sourceBone)) continue;

            // ソースのワールド回転を取得
            Quaternion sourceWorldRot = sourceWorldRotations[sourceBone];

            // ターゲットのローカル回転に変換: local = parent^-1 * world
            Quaternion targetLocalRot = Quaternion.Inverse(parentWorldRot) * sourceWorldRot;

            // カーブに設定
            SetRotationKeyframe(targetClip, bindings, targetPath, time, targetLocalRot);

            // 次のボーンの親として使用
            parentWorldRot = sourceWorldRot;
        }
    }

    /// <summary>
    /// 回転キーフレームを設定
    /// </summary>
    private void SetRotationKeyframe(AnimationClip clip, EditorCurveBinding[] bindings, string path, float time, Quaternion rotation)
    {
        var rotX = bindings.FirstOrDefault(b => b.path == path && b.propertyName == "m_LocalRotation.x");
        var rotY = bindings.FirstOrDefault(b => b.path == path && b.propertyName == "m_LocalRotation.y");
        var rotZ = bindings.FirstOrDefault(b => b.path == path && b.propertyName == "m_LocalRotation.z");
        var rotW = bindings.FirstOrDefault(b => b.path == path && b.propertyName == "m_LocalRotation.w");

        if (rotX.path == null) return;

        AnimationCurve curveX = AnimationUtility.GetEditorCurve(clip, rotX);
        AnimationCurve curveY = AnimationUtility.GetEditorCurve(clip, rotY);
        AnimationCurve curveZ = AnimationUtility.GetEditorCurve(clip, rotZ);
        AnimationCurve curveW = AnimationUtility.GetEditorCurve(clip, rotW);

        // 既存のキーを探して更新
        for (int i = 0; i < curveX.keys.Length; i++)
        {
            if (Mathf.Approximately(curveX.keys[i].time, time))
            {
                curveX.MoveKey(i, new Keyframe(time, rotation.x, curveX.keys[i].inTangent, curveX.keys[i].outTangent));
                curveY.MoveKey(i, new Keyframe(time, rotation.y, curveY.keys[i].inTangent, curveY.keys[i].outTangent));
                curveZ.MoveKey(i, new Keyframe(time, rotation.z, curveZ.keys[i].inTangent, curveZ.keys[i].outTangent));
                curveW.MoveKey(i, new Keyframe(time, rotation.w, curveW.keys[i].inTangent, curveW.keys[i].outTangent));
                break;
            }
        }

        AnimationUtility.SetEditorCurve(clip, rotX, curveX);
        AnimationUtility.SetEditorCurve(clip, rotY, curveY);
        AnimationUtility.SetEditorCurve(clip, rotZ, curveZ);
        AnimationUtility.SetEditorCurve(clip, rotW, curveW);
    }

    /// <summary>
    /// ボーンパスを変換（階層構造を考慮）
    /// </summary>
    private string ConvertBonePath(string originalPath)
    {
        if (string.IsNullOrEmpty(originalPath))
            return originalPath;

        // パスを分割（例: "Center/Spine.1/Spine.2" → ["Center", "Spine.1", "Spine.2"]）
        string[] pathParts = originalPath.Split('/');
        List<string> convertedParts = new List<string>();

        bool hasMapping = false;

        // 各パーツをマッピング
        for (int i = 0; i < pathParts.Length; i++)
        {
            if (boneMapping.ContainsKey(pathParts[i]))
            {
                // マッピングがある場合は変換後の名前を追加
                convertedParts.Add(boneMapping[pathParts[i]]);
                hasMapping = true;
            }
            else
            {
                // マッピングがない場合
                // Koi固有の中間ボーン("Bone", "Center")は除外
                if (pathParts[i] != "Bone" && pathParts[i] != "Center")
                {
                    convertedParts.Add(pathParts[i]);
                }
            }
        }

        // マッピングされたボーンがない場合は元のパスを返す（変換不要）
        if (!hasMapping || convertedParts.Count == 0)
        {
            return originalPath;
        }

        // パスを再構築
        string convertedPath = string.Join("/", convertedParts);

        // Carpモデルの場合、先頭にRoot/を追加
        convertedPath = "Root/" + convertedPath;

        return convertedPath;
    }
}
