
using UnityEngine;

public class VirusShooter : MonoBehaviour
{
    [Header("射擊攝影機")]
    public Camera shootCamera;

    [Header("射擊設定")]
    public float shootRange = 100f;
    public float shootCooldown = 0.2f;

    [Header("射擊按鍵")]
    public KeyCode shootKey = KeyCode.Space;
    public bool enableMouseShooting = true;

    [Header("除錯設定")]
    public bool showDebugLog = true;
    public bool showDebugRay = true;
    public float debugRayDuration = 2f;

    private float nextShootTime = 0f;

    void Start()
    {
        if (shootCamera == null)
        {
            shootCamera = Camera.main;
        }

        if (shootCamera == null)
        {
            Debug.LogError(
                "❌ VirusShooter 找不到射擊攝影機！"
            );
        }
        else
        {
            Debug.Log(
                "✅ VirusShooter 初始化成功：" +
                shootCamera.name
            );
        }
    }

    void Update()
    {
        // 空白鍵
        bool shootPressed =
            Input.GetKeyDown(shootKey);

        // 滑鼠左鍵
        if (enableMouseShooting)
        {
            shootPressed |= Input.GetMouseButtonDown(0);
        }

        if (!shootPressed)
        {
            return;
        }

        if (showDebugLog)
        {
            Debug.Log("🔫 偵測到射擊按鍵！");
        }

        // 射擊冷卻
        if (Time.time < nextShootTime)
        {
            if (showDebugLog)
            {
                Debug.Log("⏳ 射擊冷卻中");
            }

            return;
        }

        nextShootTime =
            Time.time + shootCooldown;

        Shoot();
    }

    void Shoot()
    {
        if (shootCamera == null)
        {
            Debug.LogError(
                "❌ 無法射擊：Shoot Camera 未設定"
            );

            return;
        }

        if (showDebugLog)
        {
            Debug.Log("🔫 開始執行 Shoot()");
        }

        // 從攝影機畫面中央發射射線
        Ray ray = shootCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        // Scene 顯示綠色射線
        if (showDebugRay)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * shootRange,
                Color.green,
                debugRayDuration
            );
        }

        // Raycast 偵測
        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            shootRange,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "🎯 射線碰到物件：" +
                    hit.collider.name
                );

                Debug.Log(
                    "📍 命中位置：" +
                    hit.point
                );

                Debug.Log(
                    "📏 命中距離：" +
                    hit.distance
                );
            }

            // 尋找命中的病毒
            VirusChase virus =
                hit.collider.GetComponentInParent<VirusChase>();

            if (virus != null)
            {
                if (showDebugLog)
                {
                    Debug.Log(
                        "✅ 成功射中病毒：" +
                        virus.name
                    );
                }

                // 觸發病毒受擊
                virus.TakeHit();

                if (showDebugRay)
                {
                    Debug.DrawLine(
                        ray.origin,
                        hit.point,
                        Color.red,
                        debugRayDuration
                    );
                }
            }
            else
            {
                if (showDebugLog)
                {
                    Debug.LogWarning(
                        "⚠️ 射中其他物件：" +
                        hit.collider.name +
                        "，不是病毒"
                    );
                }
            }
        }
        else
        {
            if (showDebugLog)
            {
                Debug.LogWarning(
                    "❌ 射擊落空，沒有命中任何 Collider"
                );
            }
        }
    }
}
