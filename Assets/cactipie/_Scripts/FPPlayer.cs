using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FPController))]
public class FPPlayer : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] FPController FPController;

    void OnValidate()
    {
        if (FPController == null)
            FPController = GetComponent<FPController>();
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnMove(InputValue Value)
    {
        FPController.MoveInput = Value.Get<Vector2>();
    }

    void OnLook(InputValue Value)
    {
        FPController.LookInput = Value.Get<Vector2>();
    }

    void OnJump(InputValue Value)
    {
        if (Value.isPressed)
        {
            FPController.TryJump();
        }
    }

    void OnDash(InputValue Value)
    {
        if (Value.isPressed)
        {
            FPController.TryDash();
        }
    }

    void OnSlide(InputValue Value)
    {
        // Holding the button keeps it true, releasing makes it false
        FPController.SlideInput = Value.isPressed;
    }
}