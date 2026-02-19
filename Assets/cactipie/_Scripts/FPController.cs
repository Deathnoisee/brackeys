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
    public float StandHeight = 2f;
    public float CrouchHeight = 1f;

    [Header("Dashing")]
    public float DashSpeed = 30f;

    [Header("Jumping")]
    [SerializeField] float JumpHeight = 2f;
    [SerializeField] float JumpSpeedBoost = 4f;
    [SerializeField] bool canDoubleJump = true;
    private int timesJumped = 0;

    [Header("Look Parameters")]
    public Vector2 LookSensitivity = new Vector2(0.1f, 0.1f);
    public float pitchLimit = 85f;
    private float currentPitch = 0f;
    public float CurrentPitch
    {
        get => currentPitch;
        set => currentPitch = Mathf.Clamp(value, -pitchLimit, pitchLimit);
    }

    [Header("Camera")]
    [SerializeField] float CameraNormalFOV = 60f;
    [SerializeField] float CameraBoostFOV = 90f; // FOV expands during dash/slide
    [SerializeField] float CameraFOVSmoothing = 5f;
    [SerializeField] float CameraStandHeight = 0.8f; // Head position when standing
    [SerializeField] float CameraCrouchHeight = 0.1f; // Head position when sliding

    [Header("Physics")]
    [SerializeField] float GravityScale = 3f;
    public float VerticalVelocity = 0f;
    public Vector3 CurrentVelocity { get; private set; }
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
        CameraUpdate();

        if (!WasGrounded && IsGrounded)
        {
            timesJumped = 0;
            Landed?.Invoke();
        }

        WasGrounded = IsGrounded;
    }

    void MoveUpdate()
    {
        // 1. Determine Input Direction
        Vector3 inputDirection = transform.forward * MoveInput.y + transform.right * MoveInput.x;
        inputDirection.y = 0f;
        inputDirection.Normalize();

        bool hasInput = MoveInput.sqrMagnitude >= 0.01f;

        // 2. Calculate Speed
        if (hasInput)
        {
            moveTimer += Time.deltaTime;
            float curveValue = AccelerationCurve.Evaluate(Mathf.Clamp01(moveTimer / AccelerationTime));
            float targetSpeed = SlideInput ? SlideSpeed : RunSpeed;

            if (CurrentSpeed > targetSpeed)
            {
                // Decelerate from a Dash, Jump boost, or Slide release
                CurrentSpeed = Mathf.Lerp(CurrentSpeed, targetSpeed, DecelerationRate * Time.deltaTime);
            }
            else if (SlideInput)
            {
                // Accelerate smoothly up to slide speed
                CurrentSpeed = Mathf.Lerp(CurrentSpeed, targetSpeed, SlideAccelerationRate * Time.deltaTime);
            }
            else
            {
                // Accelerate using the Unity Curve up to max run speed
                CurrentSpeed = curveValue * RunSpeed;
            }

            // Apply direction
            CurrentVelocity = inputDirection * CurrentSpeed;
        }
        else
        {
            // Instant stop when letting go of keys
            CurrentSpeed = 0f;
            moveTimer = 0f;
            CurrentVelocity = Vector3.zero;
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
        // Only dash if we are pressing a movement key (WASD dictates direction)
        if (MoveInput.sqrMagnitude >= 0.01f)
        {
            CurrentSpeed = DashSpeed;
            // MoveUpdate will handle decelerating this smoothly back to RunSpeed
        }
    }

    void LookUpdate()
    {
        Vector2 input = new Vector2(LookInput.x * LookSensitivity.x, LookInput.y * LookSensitivity.y);

        CurrentPitch -= input.y;
        fpCamera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f);
        transform.Rotate(Vector3.up * input.x);
    }

    void CameraUpdate()
    {
        // FOV
        float targetFOV = CameraNormalFOV;
        if (CurrentSpeed > RunSpeed)
        {
            float speedRatio = (CurrentSpeed - RunSpeed) / (DashSpeed - RunSpeed);
            targetFOV = Mathf.Lerp(CameraNormalFOV, CameraBoostFOV, speedRatio);
        }
        fpCamera.Lens.FieldOfView = Mathf.Lerp(fpCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * CameraFOVSmoothing);

        //  Handle Camera Vertical Position 
        float targetCamHeight = SlideInput ? CameraCrouchHeight : CameraStandHeight;
        Vector3 camPos = fpCamera.transform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCamHeight, 10f * Time.deltaTime);
        fpCamera.transform.localPosition = camPos;
    }
}