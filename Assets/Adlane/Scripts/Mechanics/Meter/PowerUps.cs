using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PowerUps : MonoBehaviour
{
    // 3 levels of powers, at 100 points super jump like half life 1 crouch jump, at 250 Super speed for short amount of time, at 450 still to discuss for now

    [SerializeField] private MomentumMeter momentumMeter;

    private bool superJumpActive = false;
    private bool superSpeedActive = false;

    public void SuperJump (InputAction.CallbackContext context)
    {
        if (context.performed && !superJumpActive && momentumMeter.GetCurrentMeter() >= 100f)
        {
            superJumpActive = true;
                
            momentumMeter.ResetMeter(); 
        }
    }

}
