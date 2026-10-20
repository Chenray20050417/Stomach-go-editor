using UnityEngine;

public class LanePathRunner : MonoBehaviour
{
    [Header("角色")]
    public CharacterController controller;

    [Header("路徑")]
    public Transform[] pathPoints;

    [Header("前進")]
    public float forwardSpeed = 8f;

    [Header("三線")]
    public float laneDistance = 3f;
    public float laneChangeSpeed = 8f;

    // 0 = 左
    // 1 = 中
    // 2 = 右
    private int currentLane = 1;

    private float currentLaneOffset = 0f;

    [Header("轉彎")]
    public float rotationSpeed = 5f;

    [Header("Waypoint")]
    public float pointReachDistance = 1.5f;

    // 從 Point01 開始前往
    private int currentPointIndex = 1;

    void Start()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        currentLane = 1;
        currentLaneOffset = 0f;
    }

    void Update()
    {
        HandleLaneInput();
        MoveAlongPath();
    }

    // ==================================
    // 左右三線控制
    // ==================================
    void HandleLaneInput()
    {
        if (Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentLane > 0)
            {
                currentLane--;
            }
        }

        if (Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentLane < 2)
            {
                currentLane++;
            }
        }
    }

    // ==================================
    // 沿 Waypoint 前進
    // ==================================
    void MoveAlongPath()
    {
        if (pathPoints == null)
        {
            return;
        }

        if (pathPoints.Length < 2)
        {
            return;
        }

        // 路徑走完
        if (currentPointIndex >= pathPoints.Length)
        {
            return;
        }

        Transform previousPoint =
            pathPoints[currentPointIndex - 1];

        Transform nextPoint =
            pathPoints[currentPointIndex];

        // ==================================
        // 計算目前道路的前進方向
        // ==================================

        Vector3 roadForward =
            nextPoint.position -
            previousPoint.position;

        // 不改變玩家高度
        roadForward.y = 0f;

        if (roadForward.sqrMagnitude < 0.001f)
        {
            return;
        }

        roadForward.Normalize();

        // ==================================
        // 算出道路的右方向
        // ==================================

        Vector3 roadRight =
            Vector3.Cross(
                Vector3.up,
                roadForward
            ).normalized;

        // ==================================
        // 左中右三線
        //
        // 左 = -3
        // 中 = 0
        // 右 = +3
        // ==================================

        float targetLaneOffset =
            (currentLane - 1) *
            laneDistance;

        // 平滑換線
        currentLaneOffset =
            Mathf.MoveTowards(
                currentLaneOffset,
                targetLaneOffset,
                laneChangeSpeed *
                Time.deltaTime
            );

        // ==================================
        // 算目標位置
        // ==================================

        Vector3 targetPosition =
            nextPoint.position +
            roadRight *
            currentLaneOffset;

        // 你的玩家現在不受重力影響
        // 所以保持現在的 Y
        targetPosition.y =
            transform.position.y;

        // ==================================
        // 往目標前進
        // ==================================

        Vector3 direction =
            targetPosition -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();

            Vector3 movement =
                direction *
                forwardSpeed *
                Time.deltaTime;

            controller.Move(movement);
        }

        // ==================================
        // 玩家跟著道路轉向
        // ==================================

        Quaternion targetRotation =
            Quaternion.LookRotation(
                roadForward,
                Vector3.up
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );

        // ==================================
        // 到達 Waypoint
        // ==================================

        Vector3 distanceCheck =
            targetPosition -
            transform.position;

        distanceCheck.y = 0f;

        if (distanceCheck.magnitude
            <= pointReachDistance)
        {
            currentPointIndex++;
        }
    }

    // ==================================
    // Scene 畫出路徑
    // ==================================

    void OnDrawGizmos()
    {
        if (pathPoints == null)
        {
            return;
        }

        if (pathPoints.Length < 2)
        {
            return;
        }

        for (int i = 0;
             i < pathPoints.Length - 1;
             i++)
        {
            if (pathPoints[i] == null ||
                pathPoints[i + 1] == null)
            {
                continue;
            }

            Vector3 start =
                pathPoints[i].position;

            Vector3 end =
                pathPoints[i + 1].position;

            Vector3 forward =
                end - start;

            forward.y = 0f;

            if (forward.sqrMagnitude <
                0.001f)
            {
                continue;
            }

            forward.Normalize();

            Vector3 right =
                Vector3.Cross(
                    Vector3.up,
                    forward
                ).normalized;

            // 中線
            Gizmos.color = Color.yellow;

            Gizmos.DrawLine(
                start,
                end
            );

            // 左線
            Gizmos.color = Color.blue;

            Gizmos.DrawLine(
                start -
                right * laneDistance,

                end -
                right * laneDistance
            );

            // 右線
            Gizmos.color = Color.red;

            Gizmos.DrawLine(
                start +
                right * laneDistance,

                end +
                right * laneDistance
            );
        }
    }
}