
using UnityEngine;

public class StomachSegmentPulse : MonoBehaviour
{
    [Header("蠕動速度")]
    public float speed = 1.2f;

    [Header("左右擺動幅度")]
    public float sideMoveAmount = 0.05f;

    [Header("左右收縮幅度")]
    public float scaleAmount = 0.03f;

    [Header("蠕動時間差")]
    public float phaseOffset = 0f;

    private Vector3 originalPosition;
    private Vector3 originalScale;

    void Awake()
    {
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
    }

    void Update()
    {
        float wave = Mathf.Sin(
            Time.time * speed + phaseOffset
        );

        // 左右擺動 X 軸
        float offsetX = wave * sideMoveAmount;

        transform.localPosition =
            originalPosition +
            new Vector3(offsetX, 0f, 0f);

        // 左右輕微收縮
        float scaleX = 1f + wave * scaleAmount;

        transform.localScale = new Vector3(
            originalScale.x * scaleX,
            originalScale.y,
            originalScale.z
        );
    }

    void OnDisable()
    {
        transform.localPosition = originalPosition;
        transform.localScale = originalScale;
    }
}
