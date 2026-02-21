using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FPController))]
public class FPPlayer : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] FPController FPController;
    [SerializeField] PowerUps powerUps;

    void OnValidate()
    {
        if (FPController == null) FPController = GetComponent<FPController>();
        if (powerUps == null) powerUps = GetComponent<PowerUps>();
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        FPController.MoveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        FPController.LookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        FPController.TryJump();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        FPController.TryDash();
    }

    public void OnSlidePerformed(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();
        FPController.SlideInput = value > 0.5f;
    }
    public void OnSuperJump(InputAction.CallbackContext context)
    {
        powerUps.OnSuperJump(context);
    }


}