using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;

/// <summary>
/// MediaPipe Pose Landmarkerからランドマークデータを取得し、他のスクリプトに提供するシングルトン
/// オブジェクト指向原則: イベント購読による疎結合設計
/// </summary>
public class PoseLandmarkProvider : MonoBehaviour
{
    public static PoseLandmarkProvider Instance { get; private set; }

    [SerializeField] private Mediapipe.Unity.CustomPoseLandmarkerAnnotationController customAnnotationController;

    // バッキングフィールド（ref渡し用）
    private PoseLandmarkerResult latestResult;

    /// <summary>
    /// 最新のPose Landmarkerの検出結果
    /// </summary>
    public PoseLandmarkerResult LatestResult => latestResult;

    /// <summary>
    /// 有効なランドマークデータが存在するか
    /// </summary>
    public bool HasValidData => latestResult.poseLandmarks != null && latestResult.poseLandmarks.Count > 0;

    private void Awake()
    {
        // Singletonパターン: 複数インスタンス防止
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // CustomAnnotationControllerの自動検索（Inspector未設定時）
        if (customAnnotationController == null)
        {
            customAnnotationController = FindObjectOfType<Mediapipe.Unity.CustomPoseLandmarkerAnnotationController>();

            if (customAnnotationController == null)
            {
                Debug.LogError("[PoseLandmarkProvider] CustomPoseLandmarkerAnnotationController not found in scene!");
                return;
            }
        }

        // イベント購読: 検出結果を受け取る責任を持つ
        customAnnotationController.onPoseDetected.AddListener(OnPoseDetected);
    }

    private void OnDestroy()
    {
        // メモリリーク防止: イベント購読解除
        if (customAnnotationController != null)
        {
            customAnnotationController.onPoseDetected.RemoveListener(OnPoseDetected);
        }
    }

    /// <summary>
    /// Pose検出イベントのコールバック
    /// この責任: 最新の結果を保持する
    /// </summary>
    private void OnPoseDetected(PoseLandmarkerResult result)
    {
        // 結果のクローンを保存（元データの保護）
        result.CloneTo(ref latestResult);
        // Debug.Log($"[PoseLandmarkProvider] Pose detected! Landmarks: {latestResult.poseLandmarks?.Count ?? 0}");
    }

    /// <summary>
    /// 指定したインデックスのランドマークを取得（正規化座標）
    /// </summary>
    /// <param name="landmarkIndex">ランドマークインデックス（0-32）</param>
    /// <param name="landmark">取得したランドマーク</param>
    /// <returns>取得成功したか</returns>
    public bool TryGetLandmark(int landmarkIndex, out Mediapipe.Tasks.Components.Containers.NormalizedLandmark landmark)
    {
        landmark = default;

        if (!HasValidData) return false;
        if (landmarkIndex < 0 || landmarkIndex >= LatestResult.poseLandmarks[0].landmarks.Count) return false;

        landmark = LatestResult.poseLandmarks[0].landmarks[landmarkIndex];
        return true;
    }
}
