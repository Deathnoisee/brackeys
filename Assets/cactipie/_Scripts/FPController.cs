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
    public float DecelerationRate = 5f; // How fast you lose dash/jump boost speed

    [Header("Sliding")]
    public float SlideSpeed = 18f;
    public float SlideAccelerationRate = 4f;
    public float SlideSteerSpeed = 5f; // How much you can steer during a slide
    public float StandHeight = 2f;
    public float CrouchHeight = 1f;
    private bool isSlideLocked = false;
    public float SlideDuration = 0.8f; // How long the speed boost lasts
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
    private Vector3 dashDirection;

    [Header("Jumping")]
    [SerializeField] float JumpHeight = 2f;
    [SerializeField] float JumpSpeedBoost = 4f;
    [SerializeField] bool canDoubleJump = true;
    [SerializeField] float jumpGracePeriod = 0.15f;
    private float jumpGraceTimer = 0f;
    private int timesJumped = 0;
    public float AirControl = 15f; // How much you can steer while jumping

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
    [SerializeField] float groundCheckDistance = 0.1f;
    [SerializeField] LayerMask groundMask = ~0;
    public float VerticalVelocity = 0f;
    public Vector3 CurrentVelocity;
    public float CurrentSpeed { get; private set; }
    public bool WasGrounded = false;
    private bool isGrounded = false;
    public bool IsGrounded => isGrounded;
    public Vector3 GravityDirection { get; private set; } = Vector3.down;
    public Vector3 UpDirection => -GravityDirection;

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

    void OnValidate()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    void Start()
    {
        characterController.height = StandHeight;
    }

    void Update()
    {
        CheckGrounded();
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

        if (jumpGraceTimer > 0f)
            jumpGraceTimer -= Time.deltaTime;
    }

    void CheckGrounded()
    {
        if (jumpGraceTimer > 0f)
        {
            isGrounded = false;
            return;
        }

        float radius = characterController.radius * 0.9f;

        // Always cast from center — works for any gravity direction
        Vector3 origin = transform.position + characterController.center;

        // Cast distance = half capsule height + a little extra
        float castDistance = (characterController.height * 0.5f) + groundCheckDistance;

        isGrounded = Physics.SphereCast(
            origin,
            radius,
            GravityDirection,
            out RaycastHit hit,
            castDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    void MoveUpdate()
    {
        // Tick dash timer
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
        }

        // Use camera forward flattened to gravity plane instead of transform.forward
        // This way movement always works regardless of gravity direction
        Vector3 camForward = Vector3.ProjectOnPlane(fpCamera.transform.forward, GravityDirection).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(fpCamera.transform.right, GravityDirection).normalized;

        // Fallback if camera is looking directly along gravity axis
        if (camForward.sqrMagnitude < 0.001f)
            camForward = Vector3.ProjectOnPlane(fpCamera.transform.up, GravityDirection).normalized;
        if (camRight.sqrMagnitude < 0.001f)
            camRight = Vector3.ProjectOnPlane(Vector3.right, GravityDirection).normalized;

        Vector3 inputDirection = camForward * MoveInput.y + camRight * MoveInput.x;
        inputDirection = Vector3.ProjectOnPlane(inputDirection, GravityDirection).normalized;

        bool hasInput = MoveInput.sqrMagnitude >= 0.01f;

        // DASHING — overrides everything, locked direction
        if (isDashing)
        {
            CurrentSpeed = DashSpeed;
            CurrentVelocity = dashDirection * DashSpeed;
        }
        //SLIDING
        else if (IsGrounded && SlideInput && !isSlideLocked && (CurrentSpeed > 0.1f || hasInput))
        {
            if (!isSliding)
            {
                isSliding = true;
                slideDirection = hasInput ? inputDirection : camForward;
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
                    isSlideLocked = true; // Lock until keys are released
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
                        CurrentSpeed = RunSpeed;
                    else
                        CurrentSpeed = curveValue * RunSpeed;

                    CurrentVelocity = inputDirection * CurrentSpeed;
                }
                else
                {
                    // Instant stop ONLY if we are on the ground and not sliding
                    CurrentSpeed = 0f;
                    moveTimer = 0f;
                    slideTimer = 0f;
                    CurrentVelocity = Vector3.zero;
                    if (!hasInput) isSlideLocked = false; // Reset the slide lock when hands leave keys
                }
            }
            // JUMPING/FALLING
            else
            {
                if (hasInput)
                {
                    // Allow slight steering in the air without instantly changing direction
                    Vector3 targetVelocity = inputDirection * CurrentSpeed;
                    CurrentVelocity = Vector3.MoveTowards(CurrentVelocity, targetVelocity, AirControl * Time.deltaTime);
                }
                // IF NO INPUT: We do absolutely nothing to CurrentVelocity horizontally. 

            }
        }

        // Handle Gravity
        if (IsGrounded && VerticalVelocity <= 0.01f)
        {
            // Clamp to small negative value to keep grounded — not a big force
            VerticalVelocity = -1f;
        }
        else if (!IsGrounded)
        {
            VerticalVelocity -= GravityStrength * Time.deltaTime;
        }
        // If grounded and VerticalVelocity > 0 (jumping) — let it run freely

        Vector3 gravityComponent = VerticalVelocity > 0f
            ? UpDirection * VerticalVelocity
            : GravityDirection * Mathf.Abs(VerticalVelocity);

        // Strip gravity axis from movement — critical for sideways gravity
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CurrentVelocity, GravityDirection);

        // Only add gravity component when not grounded, or when grounding force is tiny
        Vector3 fullVelocity;
        if (IsGrounded && VerticalVelocity < 0f)
        {
            // Grounded — dont add gravity component to avoid sliding sideways
            fullVelocity = horizontalVelocity;
        }
        else
        {
            fullVelocity = horizontalVelocity + gravityComponent;
        }

        CollisionFlags flags = characterController.Move(fullVelocity * Time.deltaTime);

        // Ceiling bonk — SphereCast in UpDirection
        if (VerticalVelocity > 0.01f)
        {
            float radius = characterController.radius * 0.9f;
            Vector3 origin = transform.position + characterController.center;
            float castDistance = (characterController.height * 0.5f) + 0.1f;

            if (Physics.SphereCast(origin, radius, UpDirection, out RaycastHit hit, castDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                VerticalVelocity = 0f;
            }
        }

        if (!isDashing)
            CurrentSpeed = CurrentVelocity.magnitude;
    }

    void HandleSlidingHeight()
    {
        float targetHeight = SlideInput ? CrouchHeight : StandHeight;
        characterController.height = Mathf.Lerp(characterController.height, targetHeight, 10f * Time.deltaTime);

        // Correctly calculates the center
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

        // Grace period prevents ground check from killing jump immediately
        jumpGraceTimer = jumpGracePeriod;
        isGrounded = false;

        if (MoveInput.sqrMagnitude > 0.01f)
            CurrentSpeed += JumpSpeedBoost;
    }

    public void TryDash()
    {
        if (dashCooldownTimer > 0f) return;

        // Use camera-relative directions for dash too
        Vector3 camForward = Vector3.ProjectOnPlane(fpCamera.transform.forward, GravityDirection).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(fpCamera.transform.right, GravityDirection).normalized;

        Vector3 inputDir = camForward * MoveInput.y + camRight * MoveInput.x;
        inputDir = Vector3.ProjectOnPlane(inputDir, GravityDirection).normalized;

        if (inputDir.sqrMagnitude < 0.01f) return;

        dashDirection = inputDir;
        isDashing = true;
        dashTimer = DashDuration;
        dashCooldownTimer = DashCooldown;
        CurrentSpeed = DashSpeed;
    }

    void LookUpdate()
    {
        Vector2 input = new Vector2(LookInput.x * LookSensitivity.x, LookInput.y * LookSensitivity.y);

        CurrentPitch -= input.y;
        fpCamera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f);

        // Always rotate around world Y for yaw — keeps transform.forward on the XZ plane
        // so camera projections onto any gravity plane always give valid movement directions
        transform.Rotate(Vector3.up * input.x, Space.World);
    }

    public void SetGravityDirection(Vector3 newGravityDir)
    {
        GravityDirection = newGravityDir.normalized;
        VerticalVelocity = 0f;
        CurrentVelocity = Vector3.zero;

        // Reset grace timer so new gravity ground check starts fresh
        jumpGraceTimer = jumpGracePeriod;
    }
}