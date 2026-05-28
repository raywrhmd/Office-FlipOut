using UnityEngine;
using OfficeFlipOut.UI;
using OfficeFlipOut.Systems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MouseLook : MonoBehaviour
{
    [Tooltip("Degrees per pixel of mouse movement. Input System delta is already per-frame.")]
    [Range(0.01f, 0.5f)]
    public float lookSensitivity = 0.1f;
    public Transform playerBody;

    float xRotation = 0f;

    void Awake()
    {
        if (lookSensitivity > 1f)
        {
            lookSensitivity = 0.1f;
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (ClipboardUIState.ShouldBlockGameplayInput)
        {
            if (GameRuntimeState.IsCinematicInputLocked)
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                return;
            }

            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float mouseX = 0f;
        float mouseY = 0f;
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 lookDelta = mouse.delta.ReadValue();
            mouseX = lookDelta.x * lookSensitivity;
            mouseY = lookDelta.y * lookSensitivity;
        }
#endif

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        playerBody.Rotate(Vector3.up * mouseX);
    }
}

