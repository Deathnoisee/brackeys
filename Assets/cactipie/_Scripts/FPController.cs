using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class FPController : MonoBehaviour
{
    [Header("Movement Parameters")]
    public float Acceleration = 25f;          //  increased a bit for snappier feel overall (tune down if too quick)
    [SerializeField] float Deceleration = 60f; //  NEW: much higher for quick stops (40�80 range works well)
    [SerializeField] float RunSpeed = 8f;
    [SerializeField] float JumpHeight = 2f;
    private int timesJumped = 0;
    [SerializeField] bool canDoubleJump = true;

    [Header("Dash Parameters")]
    [SerializeField] float DashSpeed = 25f;
    [SerializeField] float DashDuration = 0.2f;
    [SerializeField] float DashCooldown = 0.8f;
    private float dashTimeLeft = 0f;
    private float dashCooldownLeft = 0f;
    private bool isDashing = false;

    [Header("Look Parameters")]
    public Vector2 LookSensitivity = new Vector2(0.1f, 0.1f);
    public float pitchLimit = 85f;
    [SerializeField] float currentPitch = 0f;
    public float CurrentPitch
    {
        get => currentPitch;
        set
        {
            currentPitch = Mathf.Clamp(value, -pitchLimit, pitchLimit);
        }
    }

    [Header("Camera")]
    [SerializeField] float CameraNormalFOV = 60f;
    [SerializeField] float CameraSprintFOV = 80f;
    [SerializeField] float CameraFOVSmoothing = 1f;

    [Header("Physics")]
    [SerializeField] float GravityScale = 3f;
    public float VerticalVelocity = 0f;
    public Vector3 CurrentVelocity { get; private set; }
    public float CurrentSpeed { get; private set; }
    public bool WasGrounded = false;
    public bool IsGrounded => characterController.isGrounded;
    private float normalHeight;
    private float targetHeight;

    [Header("Crouch & Slide")]
    [SerializeField] float CrouchHeight = 1.2f;
    [SerializeField] float CrouchTransitionSpeed = 15f;
    [SerializeField] float CrouchSpeedMultiplier = 0.7f;
    [SerializeField] float SlideSpeedMultiplier = 2.5f;
    [SerializeField] float SlideThresholdSpeed = 4f;
    [SerializeField] float SlideBoost = 1.3f;
    public bool CrouchInput;
    private bool isCrouching = false;
    private bool isSliding = false;
    private bool wasSliding = false;
    

    [Header("Wall Climb")]
    [SerializeField] LayerMask climbableWalls;
    [SerializeField] float WallCheckDistance = 1f;
    [SerializeField] float WallCheckHeightOffset = 1.2f;
    [SerializeField] float WallClimbHeight = 2.5f;
    [SerializeField] float WallClimbPushForce = 8f;

    [Header("Input")]
    public Vector2 MoveInput;
    public Vector2 LookInput;

    [Header("Components")]
    [SerializeField] CinemachineCamera fpCamera;
    [SerializeField] CharacterController characterController;

    [Header("Events")]
    public UnityEvent Landed;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        normalHeight = characterController.height;
        targetHeight = normalHeight;
    }

    void OnValidate()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
    }

    void Update()
    {
        // CrouchUpdate();
        MoveUpdate();
        LookUpdate();
        CameraUpdate();

        if (!WasGrounded && IsGrounded)
        {
            timesJumped = 0;
            Landed.Invoke();
        }

        WasGrounded = IsGrounded;

        // DEBUG: Visualize wall rays (Scene/Game view)
        Vector3 origin = transform.position + Vector3.up * WallCheckHeightOffset;
        Vector3 fwd = transform.forward;
        Vector3 fwdLeft = Quaternion.AngleAxis(-30f, Vector3.up) * fwd;
        Vector3 fwdRight = Quaternion.AngleAxis(30f, Vector3.up) * fwd;
        Debug.DrawRay(origin, fwd * WallCheckDistance, Color.red);
        Debug.DrawRay(origin, fwdLeft * WallCheckDistance, Color.yellow);
        Debug.DrawRay(origin, fwdRight * WallCheckDistance, Color.green);
    }

    void CrouchUpdate()
    {
        bool wantsCrouch = CrouchInput;

        Vector3 rayStart = transform.position + characterController.center;
        float rayDistance = normalHeight - characterController.height + 0.1f;
        bool hasHeadroom = !Physics.Raycast(rayStart, Vector3.up, rayDistance);

        if (wantsCrouch)
        {
            if (!isCrouching)
            {
                targetHeight = CrouchHeight;
            }
        }
        else
        {
            if (isCrouching && hasHeadroom)
            {
                targetHeight = normalHeight;
            }
        }

        float newHeight = Mathf.Lerp(characterController.height, targetHeight, CrouchTransitionSpeed * Time.deltaTime);
        characterController.height = newHeight;
        characterController.center = new Vector3(0f, newHeight / 2f, 0f);
        isCrouching = newHeight < (normalHeight + CrouchHeight) * 0.5f;
    }

    void MoveUpdate()
    {
        // Dash handling
        dashCooldownLeft = Mathf.Max(0f, dashCooldownLeft - Time.deltaTime);
        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f)
            {
                isDashing = false;
            }
        }

        Vector3 motion = transform.forward * MoveInput.y + transform.right * MoveInput.x;
        motion.y = 0f;
        motion.Normalize();

        float horizSpeed = CurrentVelocity.magnitude;
        bool wantSlide = CrouchInput && IsGrounded && horizSpeed >= SlideThresholdSpeed;
        if (wantSlide)
        {
            isSliding = true;
        }
        else
        {
            isSliding = false;
        }

        if (!isDashing)
        {
            if (motion.sqrMagnitude >= 0.01f)
            {
                float targetHorizontalSpeed = RunSpeed;
                if (isSliding)
                {
                    targetHorizontalSpeed *= SlideSpeedMultiplier;
                }
                else if (isCrouching)
                {
                    targetHorizontalSpeed *= CrouchSpeedMultiplier;
                }

                // Accelerate toward desired direction (smooth start)
                CurrentVelocity = Vector3.MoveTowards(CurrentVelocity, motion * targetHorizontalSpeed, Acceleration * Time.deltaTime);
            }
            else
            {
                // No input  decelerate quickly (snappy stop)
                float decelRate = Deceleration;
                // Optional: even stronger stop when grounded (feels more "planted")
                if (IsGrounded) decelRate *= 1.5f;

                CurrentVelocity = Vector3.MoveTowards(CurrentVelocity, Vector3.zero, decelRate * Time.deltaTime);
            }
        }

        if (IsGrounded && VerticalVelocity <= 0.01f)
        {
            VerticalVelocity = -3f;
        }
        else
        {
            VerticalVelocity += Physics.gravity.y * GravityScale * Time.deltaTime;
        }

        Vector3 fullVelocity = new Vector3(CurrentVelocity.x, VerticalVelocity, CurrentVelocity.z);
        CollisionFlags flags = characterController.Move(fullVelocity * Time.deltaTime);

        if (((flags & CollisionFlags.Above) != 0) && (VerticalVelocity > 0.01f))
        {
            VerticalVelocity = 0f;
        }

        CurrentSpeed = CurrentVelocity.magnitude;

        // Slide boost on enter
        if (isSliding && !wasSliding)
        {
            CurrentVelocity *= SlideBoost;
        }
        wasSliding = isSliding;
    }

    public void TryJump()
    {
        // Wall climb check
        Vector3 origin = transform.position + Vector3.up * WallCheckHeightOffset;
        Vector3 fwd = transform.forward;
        Vector3 fwdLeft = Quaternion.AngleAxis(-30f, Vector3.up) * fwd;
        Vector3 fwdRight = Quaternion.AngleAxis(30f, Vector3.up) * fwd;
        if (Physics.Raycast(origin, fwd, WallCheckDistance, climbableWalls) ||
            Physics.Raycast(origin, fwdLeft, WallCheckDistance, climbableWalls) ||
            Physics.Raycast(origin, fwdRight, WallCheckDistance, climbableWalls))
        {
            VerticalVelocity = Mathf.Sqrt(WallClimbHeight * -2f * Physics.gravity.y * GravityScale);
            CurrentVelocity += fwd * WallClimbPushForce;
            return;
        }

        if (IsGrounded == false)
        {
            if (canDoubleJump && timesJumped < 2 && VerticalVelocity > 0.01f)
            {
                return;
            }
            if (!canDoubleJump || timesJumped >= 2)
            {
                return;
            }
        }

        VerticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Physics.gravity.y * GravityScale);
        timesJumped++;
    }

    public void TryDash()
    {
        if (dashCooldownLeft > 0f || isDashing) return;

        isDashing = true;
        dashTimeLeft = DashDuration;
        dashCooldownLeft = DashCooldown;

        Vector3 dashDir = transform.forward * MoveInput.y + transform.right * MoveInput.x;
        dashDir.y = 0f;
        if (dashDir.sqrMagnitude > 0.01f)
        {
            dashDir.Normalize();
        }
        else
        {
            dashDir = transform.forward;
            dashDir.y = 0f;
            dashDir.Normalize();
        }
        CurrentVelocity = dashDir * DashSpeed;
    }

    void LookUpdate()
    {
        Vector2 input = new Vector2(LookInput.x * LookSensitivity.x, LookInput.y * LookSensitivity.y);
        //look up and down
        CurrentPitch -= input.y;
        fpCamera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f);
        //look left and right
        transform.Rotate(Vector3.up * input.x);
    }

    void CameraUpdate()
    {
        float FOV = CameraNormalFOV;
        float fovRatio = 0f;
        if (isDashing)
        {
            fovRatio = 1f;
        }
        else if (isSliding)
        {
            fovRatio = Mathf.Clamp01(CurrentSpeed / (RunSpeed * SlideSpeedMultiplier));
        }
        else
        {
            fovRatio = Mathf.Clamp01(CurrentSpeed / RunSpeed);
        }
        FOV = Mathf.Lerp(CameraNormalFOV, CameraSprintFOV, fovRatio);
        fpCamera.Lens.FieldOfView = Mathf.Lerp(fpCamera.Lens.FieldOfView, FOV, Time.deltaTime * CameraFOVSmoothing);
    }
}