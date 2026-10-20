using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 回合管理器只呼叫 BeginInterference(winner)；此類別控制效果及恢復。
// 病毒狂暴僅影響觸發瞬間已在場上的 VirusChase，後續生成的不受影響。
public class StomachInterferenceManager : MonoBehaviour
{
    [Header("干擾時間（秒）")]
    [Min(0.1f)] public float durationSeconds = 10f;

    [Header("病毒狂暴")]
    [Min(1f)] public float virusSpeedMultiplier = 2f;

    [Header("胃液遮擋（現有場景元件）")]
    public StomachAcidEffect stomachAcidEffect;

    [Header("準心干擾：實際旋轉的攝影機 Transform")]
    public Transform cameraTransform;
    [Range(0f, 10f)] public float aimYawDegrees = 1.5f;
    [Range(0f, 10f)] public float aimPitchDegrees = 1.0f;
    [Min(0.1f)] public float aimFrequency = 2.5f;

    public bool IsRunning { get; private set; }
    public bool LastStartSucceeded { get; private set; }
    private string activeType;
    private Coroutine activeRoutine;
    private bool applyAim;
    private readonly List<VirusChase> affected = new List<VirusChase>();

    // 由 StomachRoundManager 呼叫，不自行寫 Firebase，也不負責生成病毒。
    public bool BeginInterference(string type)
    {
        if (IsRunning) { Debug.LogWarning("[Interference] 效果仍在執行"); return false; }
        LastStartSucceeded = false;
        if (type != "VIRUS_FRENZY" && type != "STOMACH_ACID" && type != "AIM_DRIFT")
        {
            Debug.LogWarning("[Interference] 未知干擾：" + type);
            return false;
        }
        if (type == "STOMACH_ACID" && stomachAcidEffect == null)
        {
            Debug.LogError("[Interference] 尚未指定 StomachAcidEffect"); return false;
        }
        if (type == "AIM_DRIFT" && cameraTransform == null)
        {
            Debug.LogError("[Interference] 尚未指定 Camera Transform"); return false;
        }
        activeType = type;
        if (type == "VIRUS_FRENZY")
        {
            affected.Clear();
            foreach (VirusChase virus in FindObjectsOfType<VirusChase>())
            {
                if (virus == null || !virus.isActiveAndEnabled || !virus.gameObject.activeInHierarchy) continue;
                affected.Add(virus);
                virus.SetSpeedMultiplier(virusSpeedMultiplier);
            }
            Debug.Log("[Interference] 石頭/病毒狂暴，當前加速數量：" + affected.Count);
        }
        else if (type == "STOMACH_ACID") stomachAcidEffect.PlayEffect();
        else if (type == "AIM_DRIFT") applyAim = true;

        IsRunning = true;
        LastStartSucceeded = true;
        activeRoutine = StartCoroutine(EndAfterDuration());
        return true;
    }

    private IEnumerator EndAfterDuration()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, durationSeconds));
        StopEffect();
    }

    // JoyConCameraLook 每幀在 Update 寫入旋轉，此處 LateUpdate 疊加小幅畫面擾動。
    // 不修改 Joy-Con 的 yaw/pitch、限制角度、反轉方向或 BetterJoy 校正。
    private void LateUpdate()
    {
        if (!applyAim || cameraTransform == null) return;
        float t = Time.unscaledTime * aimFrequency * Mathf.PI * 2f;
        float yaw = Mathf.Sin(t) * aimYawDegrees;
        float pitch = Mathf.Sin(t * 0.73f + 1.4f) * aimPitchDegrees;
        cameraTransform.localRotation *= Quaternion.Euler(pitch, yaw, 0f);
    }

    public void StopEffect()
    {
        if (activeRoutine != null) { StopCoroutine(activeRoutine); activeRoutine = null; }
        foreach (VirusChase virus in affected)
            if (virus != null) virus.SetSpeedMultiplier(1f);
        affected.Clear();
        applyAim = false;
        // 胃液原本已有自行淡出；這裡不關閉其 GameObject，以免打斷場景動畫。
        activeType = null;
        IsRunning = false;
    }

    private void OnDisable() { StopEffect(); }

    [ContextMenu("測試：病毒狂暴")]
    public void TestVirusFrenzy() { BeginInterference("VIRUS_FRENZY"); }
    [ContextMenu("測試：胃液遮擋")]
    public void TestStomachAcid() { BeginInterference("STOMACH_ACID"); }
    [ContextMenu("測試：準心干擾")]
    public void TestAimDrift() { BeginInterference("AIM_DRIFT"); }
}
