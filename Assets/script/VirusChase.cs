using UnityEngine;

public class VirusChase : MonoBehaviour
{
    [Header("追蹤目標")]
    public Transform target;

    [Header("病毒移動")]
    public float moveSpeed = 3f;

    [Header("轉向速度")]
    public float rotateSpeed = 5f;


    [Header("外圈病毒狂暴倍率（不改原始速度）")]
    [SerializeField] private float speedMultiplier = 1f;

    public void SetSpeedMultiplier(float value)
    {
        speedMultiplier = Mathf.Max(0f, value);
    }

    void Update()
    {
        if (target == null)
        {
            return;
        }


        Vector3 targetPosition =
            target.position;


        Vector3 direction =
            targetPosition -
            transform.position;


        // 朝玩家移動
        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * speedMultiplier * Time.deltaTime
            );


        // 朝向玩家
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime
                );
        }
    }


    // Firebase 生成病毒時呼叫
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        Debug.Log(
            "🦠 病毒追蹤目標設定完成：" +
            newTarget.name
        );
    }


    // =========================
    // Trigger 碰撞
    // =========================
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            "病毒 Trigger 碰到：" +
            other.gameObject.name
        );


        if (other.CompareTag("Player"))
        {
            Debug.Log(
                "🦠 病毒碰到玩家，病毒消失！"
            );


            Destroy(gameObject);
        }
    }
}