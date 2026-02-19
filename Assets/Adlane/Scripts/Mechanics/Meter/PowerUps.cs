using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PowerUps : MonoBehaviour
{
    // 3 levels of powers, at 100 points super jump like half life 1 crouch jump, at 250 Super speed for short amount of time, at 450 still to discuss for now
    [SerializeField] private float superJumpForce = 10f; 
    [SerializeField] private float jumpAngle = 45f; 
    [SerializeField] private MomentumMeter momentumMeter;
    [SerializeField] private CharacterController characterController; 

    private bool superJumpActive = false;

    public void SuperJump(InputAction.CallbackContext context)
    {
        if (context.performed && !superJumpActive && momentumMeter.GetCurrentMeter() >= 100f)
        {
            superJumpActive = true;

            Vector3 forward = transform.forward;
            Vector3 upward = Vector3.up;
            Vector3 jumpDirection = (forward + upward).normalized;

            jumpDirection = Quaternion.AngleAxis(jumpAngle, transform.right) * jumpDirection;
            Vector3 jumpVelocity = jumpDirection * superJumpForce;

            StartCoroutine(PerformSuperJump(jumpVelocity));

            momentumMeter.ResetMeter();
        }
    }
    private IEnumerator PerformSuperJump(Vector3 jumpVelocity)
    {
        float duration = 0.3f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            characterController.Move(jumpVelocity * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        superJumpActive = false;
    }
}
