using UnityEngine;

public class StomachPeristalsisManager : MonoBehaviour
{
    [Header("拖入 A B C 段")]
    public StomachSegmentPulse segmentA;
    public StomachSegmentPulse segmentB;
    public StomachSegmentPulse segmentC;

    [Header("整體蠕動設定")]
    public float speed = 1.5f;
    public float scaleAmount = 0.08f;
    public float moveAmount = 0.08f;

    [Header("相位差（越大越像波浪往前傳）")]
    public float phaseGap = 1.2f;

    void Start()
    {
        SetupSegment(segmentA, 0f);
        SetupSegment(segmentB, phaseGap);
        SetupSegment(segmentC, phaseGap * 2f);
    }

    void SetupSegment(StomachSegmentPulse segment, float phase)
    {
        if (segment == null) return;

        segment.speed = speed;
        segment.scaleAmount = scaleAmount;
       
        segment.phaseOffset = phase;
    }
}