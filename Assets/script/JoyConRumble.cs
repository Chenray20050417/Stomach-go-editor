
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class JoyConRumble : MonoBehaviour
{
    [Header("Joy-Con Rumble Settings")]
    public bool enableRumble = true;

    [Range(0f, 1f)]
    public float rumbleStrength = 0.35f;

    [Range(0.05f, 1f)]
    public float rumbleDuration = 0.15f;

    [Header("Joy-Con A Button Mapping")]
    // BetterJoy may map physical Joy-Con A to Xbox B.
    // Xbox A = 0x1000, Xbox B = 0x2000
    public bool useXboxBMapping = true;

    [Header("Controller Settings")]
    [Range(0, 3)]
    public int controllerIndex = 0;

    public bool autoFindController = true;

    [Header("Debug")]
    public bool debugLog = true;

    private const ushort XINPUT_A = 0x1000;
    private const ushort XINPUT_B = 0x2000;

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_VIBRATION
    {
        public ushort wLeftMotorSpeed;
        public ushort wRightMotorSpeed;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState(
        uint userIndex,
        out XINPUT_STATE state
    );

    [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
    private static extern uint XInputSetState(
        uint userIndex,
        ref XINPUT_VIBRATION vibration
    );

    private bool previousButton;
    private bool isRumbling;
    private float stopTime;

    void Start()
    {
        if (autoFindController)
        {
            FindController();
        }
    }

    void Update()
    {
        XINPUT_STATE state;

        uint result = XInputGetState(
            (uint)controllerIndex,
            out state
        );

        if (result == 0)
        {
            ushort targetButton =
                useXboxBMapping ? XINPUT_B : XINPUT_A;

            bool currentButton =
                (state.Gamepad.wButtons & targetButton) != 0;

            // Trigger only when the button is first pressed.
            if (currentButton && !previousButton)
            {
                if (debugLog)
                {
                    Debug.Log("Joy-Con A button pressed");
                }

                StartRumble();
            }

            previousButton = currentButton;
        }
        else
        {
            previousButton = false;
        }

        // Stop rumble automatically.
        if (isRumbling && Time.unscaledTime >= stopTime)
        {
            StopRumble();
        }
    }

    public void StartRumble()
    {
        if (!enableRumble)
            return;

        float strength = Mathf.Clamp01(rumbleStrength);

        ushort motorSpeed =
            (ushort)(strength * 65535f);

        XINPUT_VIBRATION vibration =
            new XINPUT_VIBRATION
            {
                wLeftMotorSpeed = motorSpeed,
                wRightMotorSpeed = motorSpeed
            };

        uint result = XInputSetState(
            (uint)controllerIndex,
            ref vibration
        );

        if (result == 0)
        {
            isRumbling = true;

            stopTime =
                Time.unscaledTime + rumbleDuration;

            if (debugLog)
            {
                Debug.Log("Joy-Con rumble started");
            }
        }
        else
        {
            Debug.LogWarning(
                "XInput rumble failed: " + result
            );
        }
    }

    public void StopRumble()
    {
        XINPUT_VIBRATION vibration =
            new XINPUT_VIBRATION();

        XInputSetState(
            (uint)controllerIndex,
            ref vibration
        );

        isRumbling = false;
    }

    public void FindController()
    {
        for (int i = 0; i < 4; i++)
        {
            XINPUT_STATE state;

            if (XInputGetState((uint)i, out state) == 0)
            {
                controllerIndex = i;

                if (debugLog)
                {
                    Debug.Log(
                        "XInput controller found: " + i
                    );
                }

                return;
            }
        }

        Debug.LogWarning(
            "No XInput controller found. Check BetterJoy."
        );
    }

    void OnDisable()
    {
        StopRumble();
    }

    void OnApplicationQuit()
    {
        StopRumble();
    }
}
