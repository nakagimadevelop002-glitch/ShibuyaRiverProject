using UnityEngine;
using System.Collections;

/// <summary>
/// 蝙蝠の飛行制御コンポーネント
/// EventListenerからUnityEventで呼び出され、飛行アニメーション開始と移動を実行
/// </summary>
public class BatMove : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private Transform targetPosition;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    /// <summary>
    /// 飛行を開始（EventListenerから呼び出し）
    /// </summary>
    public void StartFly()
    {
        if (animator != null)
        {
            animator.SetTrigger("ReachEvent");
        }

        if (targetPosition != null)
        {
            StartCoroutine(FlyToTarget());
        }
        else
        {
            Debug.LogWarning("[BatMove] Target position not set!");
        }
    }

    /// <summary>
    /// 目標地点まで移動
    /// </summary>
    private IEnumerator FlyToTarget()
    {
        while (Vector3.Distance(transform.position, targetPosition.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition.position,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        // 移動完了後、自身を非アクティブ化
        gameObject.SetActive(false);
    }
}
