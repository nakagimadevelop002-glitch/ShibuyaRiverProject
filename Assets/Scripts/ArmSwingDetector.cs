using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 腕振り動作を検知し、歩行状態を判定するシステム
/// 手首と肩のランドマークから周期的な上下運動を検出
/// </summary>
public class ArmSwingDetector : MonoBehaviour
{
    [Header("Landmark Indices")]
    [SerializeField] private int leftShoulderIndex = 11;
    [SerializeField] private int rightShoulderIndex = 12;
    [SerializeField] private int leftWristIndex = 15;
    [SerializeField] private int rightWristIndex = 16;

    [Header("Detection Settings")]
    [SerializeField] private float swingThreshold = 0.1f; // 腕振りと判定する最小振幅（正規化座標）
    // TODO: 周波数フィルタリング実装時に使用
    // [SerializeField] private float minFrequency = 0.5f; // 最小周波数（Hz）歩行の下限
    // [SerializeField] private float maxFrequency = 2.0f; // 最大周波数（Hz）歩行の上限
    [SerializeField] private int historyFrames = 30; // 追跡するフレーム数（約1秒分 @ 30fps）

    [Header("Events")]
    public UnityEvent onWalkingDetected; // 歩行検知時のイベント
    public UnityEvent onWalkingStopped; // 歩行停止時のイベント

    /// <summary>
    /// 現在歩行中と判定されているか
    /// </summary>
    public bool IsWalking { get; private set; }

    // パフォーマンス最適化: PoseLandmarkProviderをキャッシュ
    private PoseLandmarkProvider landmarkProvider;

    // 時系列データ: 手首のY座標履歴
    private Queue<float> leftWristYHistory = new Queue<float>();
    private Queue<float> rightWristYHistory = new Queue<float>();

    private void Start()
    {
        // 初回のみGetComponent実行、以降はキャッシュを使用
        landmarkProvider = PoseLandmarkProvider.Instance;

        if (landmarkProvider == null)
        {
            Debug.LogError("[ArmSwingDetector] PoseLandmarkProvider instance not found!");
        }
    }

    private void Update()
    {
        if (landmarkProvider == null)
        {
            Debug.LogWarning("[ArmSwingDetector] landmarkProvider is null");
            return;
        }

        if (!landmarkProvider.HasValidData)
        {
            return; // データなし時は頻繁にログを出さない
        }

        UpdateWristHistory();
        DetectWalking();
    }

    /// <summary>
    /// 手首のY座標履歴を更新
    /// 肩を基準とした相対位置で記録（体のサイズ差を正規化）
    /// </summary>
    private void UpdateWristHistory()
    {
        // 左手首と左肩の取得
        if (landmarkProvider.TryGetLandmark(leftWristIndex, out var leftWrist) &&
            landmarkProvider.TryGetLandmark(leftShoulderIndex, out var leftShoulder))
        {
            float relativeY = leftWrist.y - leftShoulder.y; // 肩を基準とした相対Y座標
            leftWristYHistory.Enqueue(relativeY);
        }

        // 右手首と右肩の取得
        if (landmarkProvider.TryGetLandmark(rightWristIndex, out var rightWrist) &&
            landmarkProvider.TryGetLandmark(rightShoulderIndex, out var rightShoulder))
        {
            float relativeY = rightWrist.y - rightShoulder.y;
            rightWristYHistory.Enqueue(relativeY);
        }

        // 履歴サイズ制限: 古いデータを削除
        while (leftWristYHistory.Count > historyFrames)
        {
            leftWristYHistory.Dequeue();
        }
        while (rightWristYHistory.Count > historyFrames)
        {
            rightWristYHistory.Dequeue();
        }
    }

    /// <summary>
    /// 腕振り動作から歩行状態を判定
    /// 条件: 左右の腕が閾値以上に動いているか
    /// </summary>
    private void DetectWalking()
    {
        if (leftWristYHistory.Count < historyFrames || rightWristYHistory.Count < historyFrames)
        {
            return; // データ不足時は判定しない
        }

        // 左右の腕の振幅を計算
        float leftAmplitude = CalculateAmplitude(leftWristYHistory);
        float rightAmplitude = CalculateAmplitude(rightWristYHistory);

        // Debug.Log($"[ArmSwingDetector] Left: {leftAmplitude:F3}, Right: {rightAmplitude:F3}, Threshold: {swingThreshold}");

        // 歩行判定: 少なくとも片方の腕が閾値以上に動いている
        bool isCurrentlyWalking = (leftAmplitude > swingThreshold) || (rightAmplitude > swingThreshold);

        // 状態変化時のイベント発火
        if (isCurrentlyWalking && !IsWalking)
        {
            IsWalking = true;
            onWalkingDetected?.Invoke();
            // Debug.Log("[ArmSwingDetector] Walking detected!");
        }
        else if (!isCurrentlyWalking && IsWalking)
        {
            IsWalking = false;
            onWalkingStopped?.Invoke();
            // Debug.Log("[ArmSwingDetector] Walking stopped.");
        }
    }

    /// <summary>
    /// 時系列データから振幅（最大値 - 最小値）を計算
    /// </summary>
    private float CalculateAmplitude(Queue<float> history)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (float value in history)
        {
            if (value < min) min = value;
            if (value > max) max = value;
        }

        return max - min;
    }

    /// <summary>
    /// デバッグ用: 現在の振幅を取得
    /// </summary>
    public void GetCurrentAmplitudes(out float leftAmplitude, out float rightAmplitude)
    {
        leftAmplitude = leftWristYHistory.Count >= historyFrames ? CalculateAmplitude(leftWristYHistory) : 0f;
        rightAmplitude = rightWristYHistory.Count >= historyFrames ? CalculateAmplitude(rightWristYHistory) : 0f;
    }
}
