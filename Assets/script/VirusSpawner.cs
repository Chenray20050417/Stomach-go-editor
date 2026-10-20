
using UnityEngine;

public class VirusSpawner : MonoBehaviour
{
    [Header("病毒設定")]
    public GameObject virusPrefab;

    [Header("玩家設定")]
    public Transform player;

    [Header("生成設定")]
    public float spawnDistance = 6f;
    public float spawnHeightOffset = 1f;
    public float minimumSpawnDistance = 2f;

    [Header("碰撞安全設定")]
    public float virusRadius = 0.6f;
    public float wallClearance = 0.8f;
    public LayerMask environmentMask;

    [Header("測試設定")]
    public KeyCode spawnKey = KeyCode.P;

    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnVirus();
        }
    }

    public void SpawnVirus()
    {
        if (virusPrefab == null || player == null)
        {
            Debug.LogWarning("請設定 Virus Prefab 和 Player");
            return;
        }

        // 玩家前方生成方向（Z 軸）
        Vector3 direction = player.forward.normalized;

        Vector3 origin =
            player.position +
            Vector3.up * spawnHeightOffset;

        float finalDistance = spawnDistance;

        // 偵測前方牆壁
        if (Physics.SphereCast(
            origin,
            virusRadius,
            direction,
            out RaycastHit hit,
            spawnDistance,
            environmentMask))
        {
            finalDistance =
                hit.distance -
                virusRadius -
                wallClearance;

            if (finalDistance < minimumSpawnDistance)
            {
                Debug.LogWarning("前方空間不足，取消生成病毒");
                return;
            }
        }

        Vector3 spawnPosition =
            origin + direction * finalDistance;

        // 檢查生成位置
        if (Physics.CheckSphere(
            spawnPosition,
            virusRadius,
            environmentMask))
        {
            bool found = false;

            for (float distance = finalDistance;
                 distance >= minimumSpawnDistance;
                 distance -= 0.5f)
            {
                Vector3 testPosition =
                    origin + direction * distance;

                if (!Physics.CheckSphere(
                    testPosition,
                    virusRadius,
                    environmentMask))
                {
                    spawnPosition = testPosition;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning("找不到安全生成位置");
                return;
            }
        }

        // 讓病毒正面朝向玩家
        Vector3 lookDirection =
            player.position - spawnPosition;

        lookDirection.y = 0f;

        Quaternion spawnRotation = Quaternion.identity;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            spawnRotation =
                Quaternion.LookRotation(lookDirection);
        }

        // 生成病毒
        GameObject newVirus = Instantiate(
            virusPrefab,
            spawnPosition,
            spawnRotation
        );

        // 設定追蹤目標
        VirusChase chase =
            newVirus.GetComponent<VirusChase>();

        if (chase == null)
        {
            chase =
                newVirus.GetComponentInChildren<VirusChase>();
        }

        if (chase != null)
        {
            chase.SetTarget(player);
        }

        Debug.Log("病毒生成成功：" + newVirus.name);
    }

    // Scene 顯示生成位置
    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Vector3 previewPosition =
            player.position +
            Vector3.up * spawnHeightOffset +
            player.forward.normalized * spawnDistance;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            previewPosition,
            virusRadius
        );
    }
}
