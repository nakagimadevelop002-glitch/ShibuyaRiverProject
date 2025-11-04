using UnityEngine;
using UnityEngine.Events;
using Mediapipe.Tasks.Vision.PoseLandmarker;

namespace Mediapipe.Unity
{
    /// <summary>
    /// PoseLandmarkerResultAnnotationControllerを拡張し、検出結果をUnityEventで通知
    /// オブジェクト指向原則: Tell, Don't Ask - 結果を公開せず、イベントで通知
    /// </summary>
    public class CustomPoseLandmarkerAnnotationController : PoseLandmarkerResultAnnotationController
    {
        [System.Serializable]
        public class PoseLandmarkerResultEvent : UnityEvent<PoseLandmarkerResult> { }

        [Header("Custom Events")]
        /// <summary>
        /// Pose検出時に発火するイベント
        /// 購読者に検出結果を通知する責任を持つ
        /// </summary>
        public PoseLandmarkerResultEvent onPoseDetected = new PoseLandmarkerResultEvent();

        private PoseLandmarkerResult lastNotifiedResult;

        /// <summary>
        /// DrawNowをオーバーライドし、イベント発火を追加
        /// IMAGE/VIDEOモードで使用される
        /// </summary>
        public new void DrawNow(PoseLandmarkerResult target)
        {
            base.DrawNow(target);
            // Debug.Log("[CustomPoseLandmarkerAnnotationController] DrawNow called, firing event");
            onPoseDetected?.Invoke(target);
        }

        /// <summary>
        /// SyncNowをオーバーライドして描画後にイベント発火
        /// LIVE_STREAMモードではLateUpdateから呼ばれる
        /// </summary>
        protected override void SyncNow()
        {
            base.SyncNow();

            // Reflectionで親クラスの_currentTargetフィールドにアクセス
            var field = typeof(PoseLandmarkerResultAnnotationController).GetField("_currentTarget",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                var result = (PoseLandmarkerResult)field.GetValue(this);
                if (result.poseLandmarks != null && result.poseLandmarks.Count > 0)
                {
                    // Debug.Log("[CustomPoseLandmarkerAnnotationController] SyncNow called, firing event");
                    onPoseDetected?.Invoke(result);
                }
            }
        }
    }
}
