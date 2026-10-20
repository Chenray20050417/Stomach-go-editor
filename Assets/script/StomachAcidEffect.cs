
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class StomachAcidEffect : MonoBehaviour
{
    [Header("UI 物件")]
    public CanvasGroup acidGroup;
    public RectTransform dynamicDrops;

    [Header("低多邊形胃液素材")]
    public Sprite dripSprite;
    public Sprite dropSprite;
    public Sprite splashSprite;

    [Header("最後的鏡頭沾染圖片")]
    public Sprite screenStainSprite;

    [Header("液滴生成")]
    [Range(1, 12)]
    public int dropCount = 5;
    public float spawnInterval = 0.35f;

    [Header("胃液拉長")]
    public float minStretchTime = 0.4f;
    public float maxStretchTime = 0.9f;
    public float minStretchLength = 100f;
    public float maxStretchLength = 260f;

    [Header("液滴掉落")]
    public float gravity = 1100f;
    public float minFallSpeed = 80f;
    public float maxFallSpeed = 160f;

    [Header("噴濺粒子")]
    public int splashCount = 6;
    public float splashSpeed = 230f;
    public float splashLife = 0.5f;

    [Header("鏡頭沾染")]
    [Range(1, 100)]
    public int stainCount = 4;

    public float minStainSize = 160f;
    public float maxStainSize = 300f;

    public float stainFadeIn = 0.15f;
    public float stainStayTime = 1.2f;
    public float stainFadeOut = 1.2f;

    [Header("整體淡入淡出")]
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 1f;
    public float stayTime = 0.5f;

    private readonly List<GameObject> spawnedObjects =
        new List<GameObject>();

    private Coroutine effectRoutine;

    void Awake()
    {
        if (acidGroup != null)
        {
            acidGroup.alpha = 0f;
            acidGroup.interactable = false;
            acidGroup.blocksRaycasts = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayEffect();
        }
    }

    // 按 R 重新開始整套特效
    public void PlayEffect()
    {
        if (acidGroup == null ||
            dynamicDrops == null ||
            dripSprite == null ||
            dropSprite == null ||
            splashSprite == null ||
            screenStainSprite == null)
        {
            Debug.LogWarning(
                "胃液特效：請先指定所有 UI 與 Sprite！"
            );
            return;
        }

        StopAllCoroutines();
        ClearAllObjects();

        acidGroup.alpha = 0f;

        effectRoutine = StartCoroutine(
            EffectSequence()
        );
    }

    IEnumerator EffectSequence()
    {
        // 第一階段：主框淡入
        yield return FadeGroup(
            0f, 1f, fadeInTime
        );

        // 第二階段：依序生成液滴
        for (int i = 0; i < dropCount; i++)
        {
            StartCoroutine(AnimateDrop());

            yield return new WaitForSeconds(
                Mathf.Max(0f, spawnInterval) *
                Random.Range(0.75f, 1.25f)
            );
        }

        // 等待液滴與噴濺大致完成
        float fallHeight =
            dynamicDrops.rect.height * 0.75f;

        float fallTime = Mathf.Sqrt(
            2f * fallHeight /
            Mathf.Max(1f, gravity)
        );

        yield return new WaitForSeconds(
            Mathf.Max(0f, maxStretchTime) +
            fallTime +
            Mathf.Max(0f, splashLife) +
            0.4f
        );

        // 第三階段：畫面隨機出現胃液沾染
        yield return StartCoroutine(
            ShowStainSequence()
        );

        // 額外停留
        yield return new WaitForSeconds(
            Mathf.Max(0f, stayTime)
        );

        // 最後主框消失
        yield return FadeGroup(
            acidGroup.alpha,
            0f,
            fadeOutTime
        );

        ClearAllObjects();
        effectRoutine = null;
    }

    // 單顆胃液：伸長、脫離、掉落、噴濺
    IEnumerator AnimateDrop()
    {
        float width = dynamicDrops.rect.width;
        float height = dynamicDrops.rect.height;

        float x = Random.Range(
            -width * 0.38f,
             width * 0.38f
        );

        float stretchTime = Mathf.Max(
            0.05f,
            Random.Range(
                Mathf.Min(minStretchTime, maxStretchTime),
                Mathf.Max(minStretchTime, maxStretchTime)
            )
        );

        float stretchLength = Random.Range(
            Mathf.Min(minStretchLength, maxStretchLength),
            Mathf.Max(minStretchLength, maxStretchLength)
        );

        float startY = Random.Range(-100f, -30f);

        // 建立液柱
        Image stem = CreateImage(
            "AcidStem",
            dripSprite,
            new Vector2(x, startY),
            new Vector2(95f, 10f)
        );

        RectTransform stemRect = stem.rectTransform;
        stemRect.pivot = new Vector2(0.5f, 1f);

        float timer = 0f;

        // 液柱逐漸拉長
        while (timer < stretchTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / stretchTime
            );

            float smooth = t * t * (3f - 2f * t);

            float wobble =
                Mathf.Sin(t * Mathf.PI * 3f) * 6f;

            stemRect.sizeDelta = new Vector2(
                95f + wobble,
                Mathf.Lerp(
                    10f,
                    stretchLength,
                    smooth
                )
            );

            yield return null;
        }

        // 液珠脫離
        Image drop = CreateImage(
            "FallingDrop",
            dropSprite,
            new Vector2(
                x,
                startY - stretchLength
            ),
            new Vector2(105f, 145f)
        );

        float releaseTime = 0f;

        while (releaseTime < 0.18f)
        {
            releaseTime += Time.deltaTime;

            float t = Mathf.Clamp01(
                releaseTime / 0.18f
            );

            stemRect.sizeDelta = new Vector2(
                95f,
                Mathf.Lerp(
                    stretchLength,
                    stretchLength * 0.65f,
                    t
                )
            );

            yield return null;
        }

        float velocity = Random.Range(
            Mathf.Min(minFallSpeed, maxFallSpeed),
            Mathf.Max(minFallSpeed, maxFallSpeed)
        );

        Vector2 position =
            drop.rectTransform.anchoredPosition;

        float landingY = -height *
            Random.Range(0.5f, 0.78f);

        float drift = Random.Range(-25f, 25f);

        // 重力加速掉落
        while (position.y > landingY)
        {
            float dt = Time.deltaTime;

            velocity += Mathf.Max(0f, gravity) * dt;

            position.y -= velocity * dt;
            position.x += drift * dt;

            drop.rectTransform.anchoredPosition =
                position;

            yield return null;
        }

        position.y = landingY;

        // 生成小型噴濺
        StartCoroutine(
            SpawnSplash(position)
        );

        RemoveObject(drop.gameObject);
        RemoveObject(stem.gameObject);
    }

    // 小型噴濺效果
    IEnumerator SpawnSplash(Vector2 origin)
    {
        List<Image> particles = new List<Image>();
        List<Vector2> velocities = new List<Vector2>();

        int count = Mathf.Max(1, splashCount);

        for (int i = 0; i < count; i++)
        {
            float size = Random.Range(28f, 75f);

            Image img = CreateImage(
                "AcidSplashParticle",
                splashSprite,
                origin,
                new Vector2(size, size)
            );

            particles.Add(img);

            Vector2 direction = new Vector2(
                Random.Range(-1f, 1f),
                Random.Range(0.15f, 1f)
            ).normalized;

            velocities.Add(
                direction *
                splashSpeed *
                Random.Range(0.6f, 1.3f)
            );
        }

        float duration = Mathf.Max(
            0.05f, splashLife
        );

        float timer = 0f;

        while (timer < duration)
        {
            float dt = Time.deltaTime;
            timer += dt;

            float t = Mathf.Clamp01(
                timer / duration
            );

            for (int i = 0; i < particles.Count; i++)
            {
                Image img = particles[i];

                if (img == null) continue;

                Vector2 velocity = velocities[i];

                velocity.y -=
                    gravity * 0.35f * dt;

                velocities[i] = velocity;

                img.rectTransform.anchoredPosition +=
                    velocity * dt;

                img.rectTransform.localScale =
                    Vector3.one *
                    (1f - t * 0.65f);

                SetImageAlpha(img, 1f - t);
            }

            yield return null;
        }

        foreach (Image img in particles)
        {
            if (img != null)
                RemoveObject(img.gameObject);
        }
    }

    // 最後階段：隨機顯示胃液痕跡
    IEnumerator ShowStainSequence()
    {
        List<Coroutine> routines =
            new List<Coroutine>();

        int count = Mathf.Max(1, stainCount);

        float longestDuration =
            Mathf.Max(0.01f, stainFadeIn) +
            Mathf.Max(0f, stainStayTime) +
            Mathf.Max(0.01f, stainFadeOut);

        // 讓每張圖稍微錯開出現
        float interval = 0.15f;

        for (int i = 0; i < count; i++)
        {
            routines.Add(
                StartCoroutine(ShowScreenStain())
            );

            if (i < count - 1)
                yield return new WaitForSeconds(interval);
        }

        // 等待最後一張痕跡消散
        yield return new WaitForSeconds(
            longestDuration
        );
    }

    // 單張 StomachAcid_Splash2 胃液沾染動畫
    IEnumerator ShowScreenStain()
    {
        float width = dynamicDrops.rect.width;
        float height = dynamicDrops.rect.height;

        // 左右隨機，避開中央準星
        bool leftSide = Random.value < 0.5f;

   float x = Random.Range(
    -width * 0.45f,
     width * 0.45f
);

    float y = Random.Range(
    -height * 0.78f,
    -height * 0.12f
);

        float size = Random.Range(
            Mathf.Min(minStainSize, maxStainSize),
            Mathf.Max(minStainSize, maxStainSize)
        );

        Image stain = CreateImage(
            "ScreenAcidStain",
            screenStainSprite,
            new Vector2(x, y),
            new Vector2(size, size)
        );

        RectTransform rect = stain.rectTransform;

        rect.localRotation = Quaternion.Euler(
            0f, 0f,
            Random.Range(-35f, 35f)
        );

        Vector3 finalScale =
            Vector3.one *
            Random.Range(0.85f, 1.15f);

        // A：快速附著螢幕
        float timer = 0f;
        float fadeIn = Mathf.Max(
            0.01f, stainFadeIn
        );

        SetImageAlpha(stain, 0f);

        while (timer < fadeIn)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / fadeIn
            );

            SetImageAlpha(stain, t);

            rect.localScale = finalScale *
                Mathf.Lerp(0.65f, 1f, t);

            yield return null;
        }

        SetImageAlpha(stain, 1f);
        rect.localScale = finalScale;

        // B：停留在鏡頭
        yield return new WaitForSeconds(
            Mathf.Max(0f, stainStayTime)
        );

        // C：緩慢向下滑動並消散
        timer = 0f;

        float fadeOut = Mathf.Max(
            0.01f, stainFadeOut
        );

        Vector2 originalPosition =
            rect.anchoredPosition;

        float slideDistance =
            Random.Range(30f, 75f);

        while (timer < fadeOut)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / fadeOut
            );

            rect.anchoredPosition =
                originalPosition +
                new Vector2(
                    0f,
                    -slideDistance * t
                );

            SetImageAlpha(stain, 1f - t);

            rect.localScale = finalScale *
                Mathf.Lerp(1f, 0.85f, t);

            yield return null;
        }

        RemoveObject(stain.gameObject);
    }

    // 建立 UI Sprite
    Image CreateImage(
        string objectName,
        Sprite sprite,
        Vector2 position,
        Vector2 size)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );

        obj.transform.SetParent(
            dynamicDrops, false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image img = obj.GetComponent<Image>();

        img.sprite = sprite;
        img.color = Color.white;
        img.raycastTarget = false;

        spawnedObjects.Add(obj);

        return img;
    }

    // 主胃液框淡入淡出
    IEnumerator FadeGroup(
        float from,
        float to,
        float duration)
    {
        duration = Mathf.Max(
            0.01f, duration
        );

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / duration
            );

            t = t * t * (3f - 2f * t);

            acidGroup.alpha =
                Mathf.Lerp(from, to, t);

            yield return null;
        }

        acidGroup.alpha = to;
    }

    void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;

        Color color = img.color;
        color.a = Mathf.Clamp01(alpha);
        img.color = color;
    }

    void RemoveObject(GameObject obj)
    {
        if (obj == null) return;

        spawnedObjects.Remove(obj);
        Destroy(obj);
    }

    void ClearAllObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedObjects.Clear();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        ClearAllObjects();

        if (acidGroup != null)
            acidGroup.alpha = 0f;
    }
}
