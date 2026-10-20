
using UnityEngine;

public class JoyConCameraLook : MonoBehaviour
{
    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Sensitivity")]
    [Range(0.1f, 10f)]
    public float horizontalSensitivity = 2f;

    [Range(0.1f, 10f)]
    public float verticalSensitivity = 2f;

    [Header("Invert Direction")]
    public bool invertX = true;
    public bool invertY = false;

    [Header("Angle Limits")]
    public float maxYaw = 60f;
    public float maxPitchUp = 45f;
    public float maxPitchDown = 45f;

    [Header("Anti Jitter")]
    [Range(0f, 0.5f)]
    public float deadZone = 0.02f;

    [Range(0.005f, 0.2f)]
    public float smoothTime = 0.03f;

    [Header("Joy-Con Recenter")]
    [Range(0, 19)]
    public int recenterButton = 0;

    public bool enableKeyboardC = true;
    public bool debugJoyConButtons = false;

    private Quaternion forwardRotation;

    private float targetYaw;
    private float targetPitch;

    private float currentYaw;
    private float currentPitch;

    private float yawVelocity;
    private float pitchVelocity;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = transform;

        // Record initial camera direction
        forwardRotation = cameraTransform.localRotation;

        RecenterView();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // BetterJoy gyro mouse input
        float x = Input.GetAxisRaw("Mouse X");
        float y = Input.GetAxisRaw("Mouse Y");

        // Dead zone
        if (Mathf.Abs(x) < deadZone)
            x = 0f;

        if (Mathf.Abs(y) < deadZone)
            y = 0f;

        // Horizontal direction
        float xDirection = invertX ? -1f : 1f;

        targetYaw +=
            x * horizontalSensitivity * xDirection;

        // Vertical direction
        float yDirection = invertY ? 1f : -1f;

        targetPitch +=
            y * verticalSensitivity * yDirection;

        // Horizontal angle limits
        targetYaw = Mathf.Clamp(
            targetYaw,
            -maxYaw,
            maxYaw
        );

        // Vertical angle limits
        targetPitch = Mathf.Clamp(
            targetPitch,
            -maxPitchUp,
            maxPitchDown
        );

        // Smooth horizontal movement
        currentYaw = Mathf.SmoothDamp(
            currentYaw,
            targetYaw,
            ref yawVelocity,
            smoothTime
        );

        // Smooth vertical movement
        currentPitch = Mathf.SmoothDamp(
            currentPitch,
            targetPitch,
            ref pitchVelocity,
            smoothTime
        );

        // Apply camera rotation
        cameraTransform.localRotation =
            forwardRotation *
            Quaternion.Euler(
                currentPitch,
                currentYaw,
                0f
            );

        // Joy-Con B button (configurable)
        KeyCode joyConButton =
            (KeyCode)((int)KeyCode.JoystickButton0
            + recenterButton);

        bool joyConRecenter =
            Input.GetKeyDown(joyConButton);

        // Optional keyboard C
        bool keyboardRecenter =
            enableKeyboardC &&
            Input.GetKeyDown(KeyCode.C);

        if (joyConRecenter || keyboardRecenter)
        {
            RecenterView();
            Debug.Log("Joy-Con view recentered");
        }

        // Find actual BetterJoy button number
        if (debugJoyConButtons)
        {
            for (int i = 0; i < 20; i++)
            {
                KeyCode button =
                    (KeyCode)((int)KeyCode.JoystickButton0 + i);

                if (Input.GetKeyDown(button))
                {
                    Debug.Log("Joy-Con Button: " + i);
                }
            }
        }
    }

    public void RecenterView()
    {
        targetYaw = 0f;
        targetPitch = 0f;

        currentYaw = 0f;
        currentPitch = 0f;

        yawVelocity = 0f;
        pitchVelocity = 0f;

        if (cameraTransform != null)
        {
            cameraTransform.localRotation =
                forwardRotation;
        }
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
