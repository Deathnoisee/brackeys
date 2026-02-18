using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class FPPlayer : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] FPController FPController;

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

    void OnCrouch(InputValue Value)
    {
        FPController.CrouchInput = Value.isPressed;
    }

    void OnDash(InputValue Value)
    {
        if (Value.isPressed)
        {
            FPController.TryDash();
        }
    }

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
}