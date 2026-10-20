
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Stomach GO 外圈干擾管理器
// 由 StomachRoundManager 呼叫 BeginInterference(winner)
// 不自行寫入 Firebase，也不負責生成病毒。

public class StomachInterferenceManager : MonoBehaviour
{
    // =========================================
    // 干擾時間
    // =========================================

    [Header("干擾時間（秒）")]
    [Min(0.1f)]
    public float durationSeconds = 10f;


    // =========================================
    // 病毒狂暴
    // =========================================

    [Header("病毒狂暴")]
    [Min(1f)]
    public float virusSpeedMultiplier = 2f;

    [Header("病毒搜尋間隔（秒）")]
    [Min(0.02f)]
    public float virusScanInterval = 0.1f;


    // =========================================
    // 胃液遮擋
    // =========================================

    [Header("胃液遮擋")]
    public StomachAcidEffect stomachAcidEffect;


    // =========================================
    // 準星飄移
    // =========================================

    [Header("準星干擾：實際旋轉的攝影機")]
    public Transform cameraTransform;

    [Range(0f, 10f)]
    public float aimYawDegrees = 1.5f;

    [Range(0f, 10f)]
    public float aimPitchDegrees = 1.0f;

    [Min(0.1f)]
    public float aimFrequency = 2.5f;


    // =========================================
    // 執行狀態
    // =========================================

    public bool IsRunning { get; private set; }

    public bool LastStartSucceeded { get; private set; }

    private string activeType;

    private Coroutine activeRoutine;
    private Coroutine virusScanRoutine;

    private bool applyAim = false;

    // 記錄已經受到狂暴影響的病毒
    private readonly HashSet<VirusChase> affected =
        new HashSet<VirusChase>();


    // =========================================
    // 開始干擾
    // =========================================

    public bool BeginInterference(string type)
    {
        if (IsRunning)
        {
            Debug.LogWarning(
                "[Interference] 效果仍在執行"
            );

            return false;
        }

        LastStartSucceeded = false;

        if (type != "VIRUS_FRENZY" &&
            type != "STOMACH_ACID" &&
            type != "AIM_DRIFT")
        {
            Debug.LogWarning(
                "[Interference] 未知干擾：" + type
            );

            return false;
        }

        if (type == "STOMACH_ACID" &&
            stomachAcidEffect == null)
        {
            Debug.LogError(
                "[Interference] 尚未指定 StomachAcidEffect"
            );

            return false;
        }

        if (type == "AIM_DRIFT" &&
            cameraTransform == null)
        {
            Debug.LogError(
                "[Interference] 尚未指定 Camera Transform"
            );

            return false;
        }

        activeType = type;

        IsRunning = true;
        LastStartSucceeded = true;


        // =====================================
        // 病毒狂暴
        // =====================================

        if (type == "VIRUS_FRENZY")
        {
            affected.Clear();

            // 先加速當前所有病毒
            ApplyVirusFrenzy();

            // 狂暴期間持續偵測新病毒
            virusScanRoutine =
                StartCoroutine(ScanNewViruses());

            Debug.Log(
                "[Interference] 病毒狂暴開始！倍率：" +
                virusSpeedMultiplier
            );
        }


        // =====================================
        // 胃液遮擋
        // =====================================

        else if (type == "STOMACH_ACID")
        {
            stomachAcidEffect.PlayEffect();

            Debug.Log(
                "[Interference] 胃液遮擋開始"
            );
        }


        // =====================================
        // 準星干擾
        // =====================================

        else if (type == "AIM_DRIFT")
        {
            applyAim = true;

            Debug.Log(
                "[Interference] 準星飄移開始"
            );
        }


        // 自動結束干擾
        activeRoutine =
            StartCoroutine(EndAfterDuration());

        return true;
    }


    // =========================================
    // 病毒狂暴：搜尋所有病毒
    // =========================================

    private void ApplyVirusFrenzy()
    {
        VirusChase[] viruses =
            FindObjectsOfType<VirusChase>();

        foreach (VirusChase virus in viruses)
        {
            if (virus == null)
                continue;

            if (!virus.isActiveAndEnabled)
                continue;

            if (!virus.gameObject.activeInHierarchy)
                continue;

            // 已加速過的病毒不重複處理
            if (affected.Contains(virus))
                continue;

            // 加入記錄
            affected.Add(virus);

            // 套用狂暴倍率
            virus.SetSpeedMultiplier(
                virusSpeedMultiplier
            );

            Debug.Log(
                "[Interference] 病毒加入狂暴：" +
                virus.name
            );
        }
    }


    // =========================================
    // 狂暴期間持續尋找新病毒
    // =========================================

    private IEnumerator ScanNewViruses()
    {
        WaitForSecondsRealtime wait =
            new WaitForSecondsRealtime(
                Mathf.Max(0.02f, virusScanInterval)
            );

        while (IsRunning &&
               activeType == "VIRUS_FRENZY")
        {
            ApplyVirusFrenzy();

            yield return wait;
        }
    }


    // =========================================
    // 干擾時間結束
    // =========================================

    private IEnumerator EndAfterDuration()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.1f, durationSeconds)
        );

        StopEffect();
    }


    // =========================================
    // 準星飄移
    // =========================================

    private void LateUpdate()
    {
        if (!applyAim || cameraTransform == null)
            return;

        float t =
            Time.unscaledTime *
            aimFrequency *
            Mathf.PI * 2f;

        float yaw =
            Mathf.Sin(t) *
            aimYawDegrees;

        float pitch =
            Mathf.Sin(t * 0.73f + 1.4f) *
            aimPitchDegrees;

        // 疊加在 Joy-Con 攝影機旋轉之後
        cameraTransform.localRotation *=
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );
    }


    // =========================================
    // 停止干擾
    // =========================================

    public void StopEffect()
    {
        // 停止時間倒數
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        // 停止搜尋新病毒
        if (virusScanRoutine != null)
        {
            StopCoroutine(virusScanRoutine);
            virusScanRoutine = null;
        }

        // 恢復所有病毒的正常速度
        foreach (VirusChase virus in affected)
        {
            if (virus != null)
            {
                virus.SetSpeedMultiplier(1f);
            }
        }

        affected.Clear();

        // 停止準星干擾
        applyAim = false;

        // 胃液本身有淡出效果，不強制關閉
        activeType = null;

        IsRunning = false;

        Debug.Log(
            "[Interference] 干擾結束，病毒速度已恢復"
        );
    }


    // =========================================
    // 物件停用時清理
    // =========================================

    private void OnDisable()
    {
        StopEffect();
    }


    // =========================================
    // Inspector 測試功能
    // =========================================

    [ContextMenu("測試：病毒狂暴")]
    public void TestVirusFrenzy()
    {
        BeginInterference("VIRUS_FRENZY");
    }

    [ContextMenu("測試：胃液遮擋")]
    public void TestStomachAcid()
    {
        BeginInterference("STOMACH_ACID");
    }

    [ContextMenu("測試：準星干擾")]
    public void TestAimDrift()
    {
        BeginInterference("AIM_DRIFT");
    }
}
