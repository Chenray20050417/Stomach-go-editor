using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float gravity = -9.81f;

    [Header("Mouse")]
    public float mouseSensitivity = 2f;

    [Header("Camera")]
    public Transform playerCamera;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("PlayerController 啟動成功");
    }

    void Update()
    {
        // =========================
        // WASD
        // =========================

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move =
            transform.right * x +
            transform.forward * z;

        // =========================
        // 重力
        // =========================

        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        move.y = verticalVelocity;

        // =========================
        // 移動
        // =========================

        controller.Move(move * moveSpeed * Time.deltaTime);

        // =========================
        // 滑鼠左右
        // =========================

        float mouseX =
            Input.GetAxis("Mouse X") * mouseSensitivity;

        transform.Rotate(
            Vector3.up * mouseX
        );

        // =========================
        // 滑鼠上下
        // =========================

        float mouseY =
            Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraPitch -= mouseY;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -80f,
            80f
        );

        if (playerCamera != null)
        {
            playerCamera.localRotation =
                Quaternion.Euler(
                    cameraPitch,
                    0f,
                    0f
                );
        }

        // =========================
        // ESC
        // =========================

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
