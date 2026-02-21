using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class FPController : MonoBehaviour
{
    [Header("Parkour Movement")]
    public float RunSpeed = 12f;
    [Tooltip("Curve X is time (0 to 1), Y is speed multiplier (0 to 1). Make it start steep and flatten out.")]
    public AnimationCurve AccelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float AccelerationTime = 0.5f;
    public float DecelerationRate = 5f;

    [Header("Sliding")]
    public float SlideSpeed = 18f;
    public float SlideAccelerationRate = 4f;
    public float SlideSteerSpeed = 5f;
    public float StandHeight = 2f;
    public float CrouchHeight = 1f;
    private bool isSlideLocked = false;
    public float SlideDuration = 0.8f;
    private float slideTimer;
    private Vector3 slideDirection;
    private bool isSliding = false;

    [Header("Dashing")]
    public float DashSpeed = 30f;
    public float DashDuration = 0.15f;
    public float DashCooldown = 1f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashVelocity = Vector3.zero;

    [Header("Jumping")]
    [SerializeField] float JumpHeight = 2f;
    [SerializeField] float JumpSpeedBoost = 4f;
    [SerializeField] bool canDoubleJump = true;
    private int timesJumped = 0;
    public float AirControl = 15f;

    [Header("Look Parameters")]
    public Vector2 LookSensitivity = new Vector2(0.1f, 0.1f);
    public float pitchLimit = 85f;
    private float currentPitch = 0f;
    public float CurrentPitch
    {
        get => currentPitch;
        set => currentPitch = Mathf.Clamp(value, -pitchLimit, pitchLimit);
    }

    [Header("Physics")]
    [SerializeField] float GravityStrength = 30f;
    [SerializeField] float groundCheckDistance = 0.3f;
    [SerializeField] LayerMask groundMask = ~0; // Everything by default
    public float VerticalVelocity = 0f;
    public Vector3 CurrentVelocity;
    public float CurrentSpeed { get; private set; }
    public bool WasGrounded = false;
    private bool isGrounded = false;
    public bool IsGrounded => isGrounded;
    public Vector3 GravityDirection { get; private set; } = Vector3.down;
    public Vector3 UpDirection => -GravityDirection;

    [Header("Gravity Rotation")]
    [SerializeField] float gravityRotationSpeed = 10f;

    [Header("Input States")]
    public Vector2 MoveInput;
    public Vector2 LookInput;
    public bool SlideInput;

    [Header("Components")]
    [SerializeField] CinemachineCamera fpCamera;
    [SerializeField] CharacterController characterController;

    [Header("Events")]
    public UnityEvent Landed;

    private float moveTimer = 0f;
    private Quaternion targetGravityRotation;
    private float jumpGroundIgnoreTimer;
    private float jumpGroundIgnoreDuration = 0.1f;

    void OnValidate()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    void Start()
    {
        characterController.height = StandHeight;
        targetGravityRotation = transform.rotation;
    }

    void Update()
    {
        CheckGrounded();
        RotateToGravity();
        HandleSlidingHeight();
        MoveUpdate();
        LookUpdate();

        if (!WasGrounded && IsGrounded)
        {
            timesJumped = 0;
            Landed?.Invoke();
        }

        WasGrounded = IsGrounded;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // Tick jump grace timer
        if (jumpGroundIgnoreTimer > 0f)
            jumpGroundIgnoreTimer -= Time.deltaTime;
    }

    void CheckGrounded()
    {
        // Grace period — skip ground check right after jumping
        if (jumpGroundIgnoreTimer > 0f)
        {
            isGrounded = false;
            return;
        }

        float radius = characterController.radius * 0.9f;
        float halfHeight = characterController.height * 0.5f;
        Vector3 origin = transform.position;

        isGrounded = Physics.SphereCast(origin, radius, GravityDirection, out RaycastHit hit, halfHeight - radius + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    void RotateToGravity()
    {
        // Smoothly rotate the player so local Y aligns with UpDirection
        Vector3 currentForward = Vector3.ProjectOnPlane(transform.forward, UpDirection);
        if (currentForward.sqrMagnitude < 0.001f)
            currentForward = Vector3.ProjectOnPlane(transform.right, UpDirection);
        currentForward.Normalize();

        targetGravityRotation = Quaternion.LookRotation(currentForward, UpDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetGravityRotation, gravityRotationSpeed * Time.deltaTime);
    }

    // Movement directions projected onto the plane perpendicular to gravity
    Vector3 GetMoveForward()
    {
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, UpDirection).normalized;
        if (fwd.sqrMagnitude < 0.001f)
            fwd = Vector3.ProjectOnPlane(Vector3.forward, UpDirection).normalized;
        return fwd;
    }

    Vector3 GetMoveRight()
    {
        Vector3 right = Vector3.ProjectOnPlane(transform.right, UpDirection).normalized;
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.ProjectOnPlane(Vector3.right, UpDirection).normalized;
        return right;
    }

    void MoveUpdate()
    {
        // Tick dash
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                dashVelocity = Vector3.zero;
                // Restore normal run speed after dash ends — no bleed
                CurrentSpeed = RunSpeed;
                CurrentVelocity = GetMoveForward() * RunSpeed * (MoveInput.sqrMagnitude > 0.01f ? 1f : 0f);
            }
        }

        // Determine Input Direction on the gravity-relative plane
        Vector3 moveForward = GetMoveForward();
        Vector3 moveRight = GetMoveRight();
        Vector3 inputDirection = moveForward * MoveInput.y + moveRight * MoveInput.x;
        inputDirection = Vector3.ProjectOnPlane(inputDirection, UpDirection).normalized;

        bool hasInput = MoveInput.sqrMagnitude >= 0.01f;

        // DASHING — skip all normal movement logic entirely
        if (isDashing)
        {
            // Only apply gravity — dash handles horizontal completely
            if (IsGrounded && VerticalVelocity <= 0.01f)
                VerticalVelocity = -3f;
            else
                VerticalVelocity -= GravityStrength * Time.deltaTime;

            Vector3 gravityVelocity = VerticalVelocity > 0f
                ? UpDirection * VerticalVelocity
                : GravityDirection * Mathf.Abs(VerticalVelocity);

            characterController.Move((dashVelocity + gravityVelocity) * Time.deltaTime);
            return; // Skip everything else
        }

        // SLIDING
        else if (IsGrounded && SlideInput && !isSlideLocked && (CurrentSpeed > 0.1f || hasInput))
        {
            if (!isSliding)
            {
                isSliding = true;
                slideDirection = hasInput ? inputDirection : moveForward;
                CurrentSpeed = SlideSpeed;
                slideTimer = 0f;
            }

            if (hasInput)
            {
                slideDirection = Vector3.Lerp(slideDirection, inputDirection, SlideSteerSpeed * Time.deltaTime).normalized;
            }

            slideTimer += Time.deltaTime;

            if (slideTimer < SlideDuration)
            {
                CurrentSpeed = SlideSpeed;
            }
            else
            {
                CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, 0f, DecelerationRate * Time.deltaTime);

                if (CurrentSpeed <= 0.01f)
                {
                    CurrentSpeed = 0f;
                    isSlideLocked = true;
                }
            }

            CurrentVelocity = slideDirection * CurrentSpeed;
        }
        else
        {
            isSliding = false;

            // GROUNDED & RUNNING
            if (IsGrounded)
            {
                if (hasInput && !isSlideLocked)
                {
                    moveTimer += Time.deltaTime;
                    float curveValue = AccelerationCurve.Evaluate(Mathf.Clamp01(moveTimer / AccelerationTime));

                    if (CurrentSpeed > RunSpeed)
                    {
                        CurrentSpeed = RunSpeed;
                    }
                    else
                    {
                        CurrentSpeed = curveValue * RunSpeed;
                    }

                    CurrentVelocity = inputDirection * CurrentSpeed;
                }
                else
                {
                    CurrentSpeed = 0f;
                    moveTimer = 0f;
                    slideTimer = 0f;
                    CurrentVelocity = Vector3.zero;

                    if (!hasInput) isSlideLocked = false;
                }
            }
            // JUMPING/FALLING
            else
            {
                if (hasInput)
                {
                    Vector3 targetVelocity = inputDirection * CurrentSpeed;
                    CurrentVelocity = Vector3.MoveTowards(CurrentVelocity, targetVelocity, AirControl * Time.deltaTime);
                }
            }
        }

        // Handle Gravity
        if (IsGrounded && VerticalVelocity <= 0.01f)
            VerticalVelocity = -3f;
        else
            VerticalVelocity -= GravityStrength * Time.deltaTime;

        Vector3 gravityVel = VerticalVelocity > 0f
            ? UpDirection * VerticalVelocity
            : GravityDirection * Mathf.Abs(VerticalVelocity);

        Vector3 fullVelocity = CurrentVelocity + gravityVel;
        characterController.Move(fullVelocity * Time.deltaTime);

        // Ceiling bonk
        if (VerticalVelocity > 0.01f)
        {
            float radius = characterController.radius * 0.9f;
            float halfHeight = characterController.height * 0.5f;

            if (Physics.SphereCast(transform.position, radius, UpDirection, out RaycastHit hit, halfHeight - radius + 0.1f, groundMask, QueryTriggerInteraction.Ignore))
                VerticalVelocity = 0f;
        }

        if (!isDashing)
            CurrentSpeed = CurrentVelocity.magnitude;
    }

    void HandleSlidingHeight()
    {
        float targetHeight = SlideInput ? CrouchHeight : StandHeight;
        characterController.height = Mathf.Lerp(characterController.height, targetHeight, 10f * Time.deltaTime);

        float currentCenterY = (characterController.height - StandHeight) / 2f;
        characterController.center = new Vector3(0, currentCenterY, 0);
    }

    public void TryJump()
    {
        if (!IsGrounded)
        {
            if (!canDoubleJump || timesJumped >= 2) return;
        }

        VerticalVelocity = Mathf.Sqrt(JumpHeight * 2f * GravityStrength);
        timesJumped++;
        jumpGroundIgnoreTimer = jumpGroundIgnoreDuration;
        isGrounded = false;

        // Only apply speed boost if not dashing
        if (MoveInput.sqrMagnitude > 0.01f && !isDashing)
        {
            CurrentSpeed = Mathf.Min(CurrentSpeed + JumpSpeedBoost, RunSpeed);
        }
    }

    public void TryDash()
    {
        if (dashCooldownTimer > 0f) return;

        Vector3 moveForward = GetMoveForward();
        Vector3 moveRight = GetMoveRight();
        Vector3 inputDir = (moveForward * MoveInput.y + moveRight * MoveInput.x).normalized;

        // No input — dash forward
        if (inputDir.sqrMagnitude < 0.01f)
            inputDir = moveForward;

        dashVelocity = inputDir * DashSpeed;
        isDashing = true;
        dashTimer = DashDuration;
        dashCooldownTimer = DashCooldown;

        // Dont touch CurrentVelocity or CurrentSpeed at all
    }

    void LookUpdate()
    {
        Vector2 input = new Vector2(LookInput.x * LookSensitivity.x, LookInput.y * LookSensitivity.y);

        CurrentPitch -= input.y;
        fpCamera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f);

        // Rotate around the player's local up (aligned with UpDirection)
        transform.Rotate(transform.up, input.x, Space.World);
    }

    public void SetGravityDirection(Vector3 newGravityDir)
    {
        GravityDirection = newGravityDir.normalized;
        VerticalVelocity = 0f;
        CurrentVelocity = Vector3.zero;
    }

    public bool IsDashing => isDashing;
}