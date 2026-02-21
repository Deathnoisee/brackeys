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
    [SerializeField] float GravityScale = 3f;
    public float VerticalVelocity = 0f;
    public Vector3 CurrentVelocity;
    public float CurrentSpeed { get; private set; }
    public bool WasGrounded = false;
    public bool IsGrounded => characterController.isGrounded;

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

        //Determine Input Direction
        Vector3 inputDirection = transform.forward * MoveInput.y + transform.right * MoveInput.x;
        inputDirection.y = 0f;
        inputDirection.Normalize();

        bool hasInput = MoveInput.sqrMagnitude >= 0.01f;

        // DASHING — overrides everything, locked direction
        if (isDashing)
        {
            CurrentSpeed = DashSpeed;
            CurrentVelocity = dashDirection * DashSpeed;
        }
        //SLIDING
        // We check if we are grounded, holding Ctrl, moving fast enough, and not locked out
        else if (IsGrounded && SlideInput && !isSlideLocked && (CurrentSpeed > 0.1f || hasInput))
        {
            if (!isSliding)
            {
                // Lock in the slide direction the moment we hit Ctrl
                isSliding = true;
                slideDirection = hasInput ? inputDirection : transform.forward;
                CurrentSpeed = SlideSpeed;
                slideTimer = 0f;
            }

            // Steer the slide direction slightly based on input
            if (hasInput)
            {
                slideDirection = Vector3.Lerp(slideDirection, inputDirection, SlideSteerSpeed * Time.deltaTime).normalized;
            }

            slideTimer += Time.deltaTime;

            if (slideTimer < SlideDuration)
            {
                // Maintain burst speed
                CurrentSpeed = SlideSpeed;
            }
            else
            {
                // Decelerate down to 0
                CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, 0f, DecelerationRate * Time.deltaTime);

                if (CurrentSpeed <= 0.01f)
                {
                    CurrentSpeed = 0f;
                    isSlideLocked = true; // Lock until keys are released
                }
            }

            // Apply movement along the (steered) slide direction
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

                    // Snap back to RunSpeed instantly if above it (no more slow decel)
                    if (CurrentSpeed > RunSpeed)
                    {
                        CurrentSpeed = RunSpeed;
                    }
                    else
                    {
                        // Normal acceleration
                        CurrentSpeed = curveValue * RunSpeed;
                    }

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
            VerticalVelocity = -3f;
        }
        else
        {
            VerticalVelocity += Physics.gravity.y * GravityScale * Time.deltaTime;
        }

        //  Move Controller
        Vector3 fullVelocity = new Vector3(CurrentVelocity.x, VerticalVelocity, CurrentVelocity.z);
        CollisionFlags flags = characterController.Move(fullVelocity * Time.deltaTime);

        // Ceiling bonk prevention
        if (((flags & CollisionFlags.Above) != 0) && (VerticalVelocity > 0.01f))
        {
            VerticalVelocity = 0f;
        }

        //Recalculate CurrentSpeed — skip during dash so it doesn't get overwritten
        if (!isDashing)
        {
            CurrentSpeed = new Vector3(CurrentVelocity.x, 0, CurrentVelocity.z).magnitude;
        }
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
            // Removed the VerticalVelocity > 0.01f check so you can double jump on the way down
            if (!canDoubleJump || timesJumped >= 2) return;
        }

        VerticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Physics.gravity.y * GravityScale);
        timesJumped++;

        // Small speed boost on jump that will naturally decelerate in MoveUpdate
        if (MoveInput.sqrMagnitude > 0.01f)
        {
            CurrentSpeed += JumpSpeedBoost;
        }
    }

    public void TryDash()
    {
        if (dashCooldownTimer > 0f) return; // On cooldown

        // Only dash if pressing a movement key
        Vector3 inputDir = transform.forward * MoveInput.y + transform.right * MoveInput.x;
        inputDir.y = 0f;
        inputDir.Normalize();

        if (inputDir.sqrMagnitude < 0.01f) return; // No input = no dash

        // Lock direction & start dash
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
        transform.Rotate(Vector3.up * input.x);
    }

}