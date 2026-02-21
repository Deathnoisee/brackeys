using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PowerUps : MonoBehaviour
{
    [Header("Super Jump")]
    [SerializeField] private float baseUpForce = 10f;
    [SerializeField] private float upForceMultiplier = 0.5f;
    [SerializeField] private float forwardForceMultiplier = 1.5f;

    [Header("References")]
    [SerializeField] private MomentumMeter momentumMeter;
    [SerializeField] private FPController fpController;

    private bool superJumpActive = false;
    private bool isButtonPressed = false;

    private void OnValidate()
    {
        if (fpController == null) fpController = GetComponent<FPController>();
    }

    public void OnSuperJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isButtonPressed = true;
        }
        else if (context.canceled && isButtonPressed)
        {
            isButtonPressed = false;

            if (!superJumpActive && momentumMeter.GetCurrentMeter() >= 100f)
            {
            superJumpActive = true;
            PerformSuperJump();
            momentumMeter.ResetMeter();
            }
        }
    }

    private void PerformSuperJump()
    {
        float currentSpeed = fpController.CurrentSpeed;

        // Use camera forward flattened to horizontal — always where you're LOOKING, not moving
        Vector3 forward = fpController.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        // Scale forces based on current velocity
        float upForce = baseUpForce + (currentSpeed * upForceMultiplier);
        float forwardForce = currentSpeed * forwardForceMultiplier;

        // Set vertical velocity for the arc — let FPController's gravity handle the rest
        fpController.VerticalVelocity = upForce;

        // Set horizontal velocity in the forward direction
        fpController.CurrentVelocity = forward * forwardForce;

        superJumpActive = false;
    }
}
