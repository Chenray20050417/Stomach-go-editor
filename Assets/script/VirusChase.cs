
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirusChase : MonoBehaviour
{
    [Header("追蹤目標")]
    public Transform target;

    [Header("病毒移動")]
    public float moveSpeed = 3f;

    [Header("轉向速度")]
    public float rotateSpeed = 5f;

    [Header("外圈病毒狂暴倍率")]
    [SerializeField] private float speedMultiplier = 1f;

    [Header("動畫設定")]
    public Animator animator;
    public string walkStateName = "preset:quadruped:walk";
    public string hitStateName = "Hit";
    public bool syncAnimationSpeed = true;

    [Header("受擊設定")]
    public float flashDuration = 0.12f;
    public float hitDuration = 0.4f;
    public float disappearDelay = 0.5f;

    private bool isDead = false;

    private struct MaterialBackup
    {
        public Material material;
        public bool hasBaseColor;
        public Color baseColor;
        public bool hasColor;
        public Color color;
        public bool hasEmission;
        public Color emissionColor;
        public bool emissionEnabled;
    }

    private readonly List<MaterialBackup> backups =
        new List<MaterialBackup>();

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        PlayWalk();
    }

    void Update()
    {
        if (isDead || target == null)
            return;

        Vector3 direction =
            target.position - transform.position;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * speedMultiplier * Time.deltaTime
        );

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRotation,
                rotateSpeed * Time.deltaTime
            );
        }
    }

    // 外圈病毒狂暴
    public void SetSpeedMultiplier(float value)
    {
        speedMultiplier = Mathf.Max(0f, value);

        if (!isDead)
            UpdateAnimationSpeed();
    }

    public void ResetSpeedMultiplier()
    {
        SetSpeedMultiplier(1f);
    }

    void UpdateAnimationSpeed()
    {
        if (animator == null) return;

        animator.speed = syncAnimationSpeed
            ? speedMultiplier
            : 1f;
    }

    void PlayWalk()
    {
        if (animator == null || isDead)
            return;

        animator.speed = 1f;
        animator.Play(walkStateName, 0, 0f);

        UpdateAnimationSpeed();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            Debug.Log("病毒追蹤：" + target.name);
            PlayWalk();
        }
    }

    // 被準星射中時呼叫
    public void TakeHit()
    {
        if (isDead) return;

        isDead = true;

        StartCoroutine(HitRoutine());
    }

    IEnumerator HitRoutine()
    {
        // 停止移動，先閃紅
        FlashRed();

        yield return new WaitForSeconds(flashDuration);

        RestoreColors();

        // 播放倒下動畫
        if (animator != null)
        {
            animator.speed = 1f;
            animator.Play(hitStateName, 0, 0f);
        }
        else
        {
            Debug.LogWarning("病毒沒有 Animator");
        }

        yield return new WaitForSeconds(hitDuration);

        // 倒下後停留一小段時間
        yield return new WaitForSeconds(disappearDelay);

        Destroy(gameObject);
    }

    void FlashRed()
    {
        backups.Clear();

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                if (mat == null) continue;

                MaterialBackup backup = new MaterialBackup();
                backup.material = mat;

                backup.hasBaseColor =
                    mat.HasProperty("_BaseColor");

                backup.hasColor =
                    mat.HasProperty("_Color");

                backup.hasEmission =
                    mat.HasProperty("_EmissionColor");

                if (backup.hasBaseColor)
                {
                    backup.baseColor =
                        mat.GetColor("_BaseColor");

                    mat.SetColor("_BaseColor", Color.red);
                }

                if (backup.hasColor)
                {
                    backup.color =
                        mat.GetColor("_Color");

                    mat.SetColor("_Color", Color.red);
                }

                if (backup.hasEmission)
                {
                    backup.emissionColor =
                        mat.GetColor("_EmissionColor");

                    backup.emissionEnabled =
                        mat.IsKeywordEnabled("_EMISSION");

                    mat.EnableKeyword("_EMISSION");

                    mat.SetColor(
                        "_EmissionColor",
                        Color.red * 3f
                    );
                }

                backups.Add(backup);
            }
        }
    }

    void RestoreColors()
    {
        foreach (MaterialBackup backup in backups)
        {
            Material mat = backup.material;

            if (mat == null) continue;

            if (backup.hasBaseColor)
                mat.SetColor("_BaseColor", backup.baseColor);

            if (backup.hasColor)
                mat.SetColor("_Color", backup.color);

            if (backup.hasEmission)
            {
                mat.SetColor(
                    "_EmissionColor",
                    backup.emissionColor
                );

                if (!backup.emissionEnabled)
                    mat.DisableKeyword("_EMISSION");
            }
        }

        backups.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("病毒碰到玩家");

            Destroy(gameObject);
        }
    }
}
