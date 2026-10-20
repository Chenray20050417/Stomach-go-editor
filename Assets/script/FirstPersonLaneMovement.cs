using UnityEngine;

public class FirstPersonLaneMovement : MonoBehaviour
{
    [Header("角色控制器")]
    public CharacterController controller;

    [Header("前進設定")]
    public float forwardSpeed = 8f;

    [Header("三線移動設定")]
    public float laneDistance = 3f;
    public float laneChangeSpeed = 10f;

    // 0 = 左線
    // 1 = 中線
    // 2 = 右線
    private int currentLane = 1;

   
    [Header("控制設定")]
    public bool canMove = true;

    void Start()
    {
        // 如果 Inspector 沒有指定
        // 自動取得 CharacterController
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        // 開場強制在中線
        currentLane = 1;
    }

    void Update()
    {
        if (!canMove)
        {
            return;
        }

        HandleLaneInput();

        HandleMovement();
    }


    // ==============================
    // 左右三線輸入
    // ==============================

    void HandleLaneInput()
    {
        // A 或左方向鍵
        if (Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveLeft();
        }


        // D 或右方向鍵
        if (Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveRight();
        }
    }


    // ==============================
    // 往左切換一條線
    // ==============================

    public void MoveLeft()
    {
        if (currentLane > 0)
        {
            currentLane--;
        }
    }


    // ==============================
    // 往右切換一條線
    // ==============================

    public void MoveRight()
    {
        if (currentLane < 2)
        {
            currentLane++;
        }
    }


    // ==============================
    // 玩家移動
    // ==============================
void HandleMovement()
{
    // ==============================
    // 三條線位置
    // ==============================

    float targetX = 0f;

    if (currentLane == 0)
    {
        targetX = -laneDistance;
    }
    else if (currentLane == 1)
    {
        targetX = 0f;
    }
    else if (currentLane == 2)
    {
        targetX = laneDistance;
    }


    // ==============================
    // 平滑左右換線
    // ==============================

    float newX = Mathf.MoveTowards(
        transform.position.x,
        targetX,
        laneChangeSpeed * Time.deltaTime
    );

    float moveX = newX - transform.position.x;


    // ==============================
    // 移動
    //
    // X = 三線左右
    // Y = 0，不受重力
    // Z = 自動前進
    // ==============================

    Vector3 movement = new Vector3(
        moveX,
        0f,
        forwardSpeed * Time.deltaTime
    );

    controller.Move(movement);
}
    // ==============================
    // 取得目前在哪一線
    // ==============================

    public int GetCurrentLane()
    {
        return currentLane;
    }


    // ==============================
    // 設定指定線
    // 0 左
    // 1 中
    // 2 右
    // ==============================

    public void SetLane(int lane)
    {
        if (lane < 0)
        {
            lane = 0;
        }

        if (lane > 2)
        {
            lane = 2;
        }

        currentLane = lane;
    }


    // ==============================
    // 停止玩家
    // ==============================

    public void StopPlayer()
    {
        canMove = false;
    }


    // ==============================
    // 恢復玩家
    // ==============================

    public void StartPlayer()
    {
        canMove = true;
    }
}