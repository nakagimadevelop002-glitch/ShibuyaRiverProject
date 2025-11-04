using UnityEngine;
using System.Collections;

/// <summary>
/// 移動しながら透明化/不透明化するStateMachineBehaviour
/// Animatorの任意のStateにアタッチして使用
/// 速度ベース移動により、State滞在時間に関係なく指定時間で必ず到達
/// </summary>
public class FadingMovementSMB : StateMachineBehaviour
{
    /// <summary>
    /// 非同期移動処理を実行するシングルトンMonoBehaviour
    /// </summary>
    class MovementExecutor : MonoBehaviour
    {
        static MovementExecutor instance;
        Coroutine currentCoroutine;

        public static MovementExecutor GetInstance()
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("FadingMovementExecutor");
                instance = obj.AddComponent<MovementExecutor>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }

        public void StartMovement(Transform target, Transform destination, Renderer renderer, MaterialPropertyBlock propertyBlock,
            float moveSpeed, float fadeSpeed, float targetAlpha, int opacityID)
        {
            // 既存のCoroutineを停止
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }

            currentCoroutine = StartCoroutine(ExecuteMovement(target, destination, renderer, propertyBlock, moveSpeed, fadeSpeed, targetAlpha, opacityID));
        }

        IEnumerator ExecuteMovement(Transform target, Transform destination, Renderer renderer, MaterialPropertyBlock propertyBlock,
            float moveSpeed, float fadeSpeed, float targetAlpha, int opacityID)
        {
            Material targetMaterial = renderer.material;
            bool isCompleted = false;

            while (!isCompleted)
            {
                // 移動処理
                Vector3 currentPosition = target.position;
                Vector3 targetPosition = destination.position;
                Vector3 direction = (targetPosition - currentPosition).normalized;
                float stepDistance = moveSpeed * Time.deltaTime;

                if (Vector3.Distance(currentPosition, targetPosition) <= stepDistance)
                {
                    target.position = targetPosition;
                }
                else
                {
                    target.position = currentPosition + direction * stepDistance;
                }

                // 透明度処理
                renderer.GetPropertyBlock(propertyBlock);
                float currentAlpha = propertyBlock.HasFloat(opacityID)
                    ? propertyBlock.GetFloat(opacityID)
                    : targetMaterial.GetFloat(opacityID);

                float alphaStep = fadeSpeed * Time.deltaTime;
                float alphaDifference = Mathf.Abs(targetAlpha - currentAlpha);

                if (alphaDifference <= alphaStep)
                {
                    propertyBlock.SetFloat(opacityID, targetAlpha);
                }
                else
                {
                    float newAlpha = currentAlpha + Mathf.Sign(targetAlpha - currentAlpha) * alphaStep;
                    propertyBlock.SetFloat(opacityID, newAlpha);
                }

                renderer.SetPropertyBlock(propertyBlock);

                // 完了判定
                bool positionReached = Vector3.Distance(target.position, destination.position) < 0.001f;
                bool alphaReached = Mathf.Abs(propertyBlock.GetFloat(opacityID) - targetAlpha) < 0.001f;

                if (positionReached && alphaReached)
                {
                    isCompleted = true;
                    break; // 完了したら即座にループ終了
                }

                yield return null;
            }

            currentCoroutine = null;
        }
    }

    [Header("移動設定")]
    [SerializeField] string targetPointName;
    [SerializeField] float moveDuration = 2f;

    [Header("透明度設定")]
    [SerializeField] float targetAlpha = 0.3f;

    Transform targetPoint;

    Transform targetTransform;      // 移動させる親Transform
    Transform animatorTransform;    // Animator本体のTransform（子）
    Renderer targetRenderer;
    Material targetMaterial;
    MaterialPropertyBlock propertyBlock;

    Vector3 startPosition;
    float startAlpha;
    float moveSpeed;           // 移動速度（単位/秒）
    float fadeSpeed;           // 透明化速度（alpha/秒）
    bool isCompleted;          // 到達完了フラグ

    static readonly int OpacityAddID = Shader.PropertyToID("_OpacityAdd");

    /// <summary>
    /// State進入時の初期化処理
    /// </summary>
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        InitializeComponents(animator);
        FindTargetPoint();
        InitializeStartValues();

        // シングルトンExecutorで非同期移動を開始
        MovementExecutor executor = MovementExecutor.GetInstance();
        executor.StartMovement(targetTransform, targetPoint, targetRenderer, propertyBlock, moveSpeed, fadeSpeed, targetAlpha, OpacityAddID);
    }

    /// <summary>
    /// LateUpdate相当: Animator処理後に子の位置を強制的にリセット（このStateの間のみ）
    /// </summary>
    public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!IsValid()) return;

        // Apply Root Motionによる子の移動を毎フレーム打ち消す
        animatorTransform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// 必要なコンポーネントを取得して初期化
    /// </summary>
    void InitializeComponents(Animator animator)
    {
        animatorTransform = animator.transform;

        // Apply Root Motionによる上書きを回避するため、親Transformを移動対象にする
        targetTransform = animatorTransform.parent;

        if (targetRenderer == null)
        {
            targetRenderer = animator.GetComponentInChildren<Renderer>();
        }

        // MaterialPropertyBlockを使用する理由: 共有マテリアル変更を避けるため
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    /// <summary>
    /// シーン内から目標地点オブジェクトを名前で検索（初回のみ）
    /// </summary>
    void FindTargetPoint()
    {
        if (targetPoint != null) return;

        if (!string.IsNullOrEmpty(targetPointName))
        {
            GameObject targetObj = GameObject.Find(targetPointName);
            if (targetObj != null)
            {
                targetPoint = targetObj.transform;
            }
        }
    }

    /// <summary>
    /// 移動・透明化の開始値を記録し、速度を計算
    /// </summary>
    void InitializeStartValues()
    {
        if (targetPoint == null) return;

        startPosition = targetTransform.position;
        startAlpha = GetCurrentAlpha();
        isCompleted = false;

        // 速度 = 距離 / 時間
        float distance = Vector3.Distance(startPosition, targetPoint.position);
        moveSpeed = distance / moveDuration;

        float alphaDifference = Mathf.Abs(targetAlpha - startAlpha);
        fadeSpeed = alphaDifference / moveDuration;
    }

    /// <summary>
    /// 現在の透明度を取得（PropertyBlock優先、フォールバックでMaterial）
    /// </summary>
    float GetCurrentAlpha()
    {
        targetRenderer.GetPropertyBlock(propertyBlock);

        // _OpacityAddプロパティから取得を試みる
        if (propertyBlock.HasFloat(OpacityAddID))
        {
            return propertyBlock.GetFloat(OpacityAddID);
        }

        // PropertyBlockが未設定の場合はMaterialから取得
        if (targetMaterial == null)
        {
            targetMaterial = targetRenderer.material;
        }

        return targetMaterial.GetFloat(OpacityAddID);
    }

    /// <summary>
    /// 必要なコンポーネントが全て有効かチェック
    /// </summary>
    bool IsValid()
    {
        return targetTransform != null && targetRenderer != null && targetPoint != null;
    }

}
