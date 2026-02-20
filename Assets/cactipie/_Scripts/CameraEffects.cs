using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Rendering;

public class CameraEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPController playerController;
    [SerializeField] private CinemachineCamera fpCamera;

    [Header("Heights & Ducking")]
    [SerializeField] float standHeight = 0.8f;
    [SerializeField] float crouchHeight = 0.1f;
    [SerializeField] float heightSmoothSpeed = 10f;

    [Header("FOV Smoothing")]
    [SerializeField] float normalFOV = 60f;
    [SerializeField] float boostFOV = 90f;
    [SerializeField] float fovSmoothTime = 0.5f; // Lower is snappier, higher is smoother
    private float fovVelocity;

    [Header("Head Bobbing")]
    [SerializeField] float bobFrequency = 12f;
    [SerializeField] float bobAmplitude = 0.05f;
    private float bobTimer;

    [Header("Landing Impact")]
    [SerializeField] float landingDipAmount = 0.4f;
    [SerializeField] float landingRecoverSpeed = 8f;
    private float currentLandingOffset;

    [Header("Motion Blur")]
    [Tooltip("Assign a Global Volume here that has Motion Blur turned up")]
    [SerializeField] Volume speedBlurVolume;
    [SerializeField] float maxBlurSpeed = 30f;

    void Start()
    {
        // Hook into the landing event from the controller
        if (playerController != null)
        {
            playerController.Landed.AddListener(TriggerLandingImpact);
        }
    }

    void Update()
    {
        if (playerController == null || fpCamera == null) return;

        HandleFOV();
        HandleCameraPosition();
        HandleMotionBlur();
    }

    void HandleFOV()
    {
        float targetFOV = normalFOV;

        // Boost FOV if we are going faster than base run speed
        if (playerController.CurrentSpeed > playerController.RunSpeed)
        {
            float speedRatio = (playerController.CurrentSpeed - playerController.RunSpeed) / (playerController.DashSpeed - playerController.RunSpeed);
            targetFOV = Mathf.Lerp(normalFOV, boostFOV, speedRatio);
        }

        // SmoothDamp has much more organic smoothing than lerp 
        fpCamera.Lens.FieldOfView = Mathf.SmoothDamp(fpCamera.Lens.FieldOfView, targetFOV, ref fovVelocity, fovSmoothTime);
    }

    void HandleCameraPosition()
    {
        //Base Height (Standing vs Sliding)
        float targetBaseHeight = playerController.SlideInput ? crouchHeight : standHeight;

        // Head Bob
        float currentBobOffset = 0f;
        if (playerController.IsGrounded && playerController.CurrentSpeed > 0.1f && !playerController.SlideInput)
        {
            // Bob faster if we are moving faster
            float speedFactor = playerController.CurrentSpeed / playerController.RunSpeed;
            bobTimer += Time.deltaTime * bobFrequency * speedFactor;

            //Sin creates up and down movements 
            currentBobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
        }
        else
        {
            //reset the sine wave when stopping
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 5f);
        }

        // Landing Recovery
        currentLandingOffset = Mathf.Lerp(currentLandingOffset, 0f, Time.deltaTime * landingRecoverSpeed);

        // Combine and Apply
        Vector3 camPos = fpCamera.transform.localPosition;
        float desiredY = targetBaseHeight + currentBobOffset + currentLandingOffset;
        camPos.y = Mathf.Lerp(camPos.y, desiredY, Time.deltaTime * heightSmoothSpeed);

        fpCamera.transform.localPosition = camPos;
    }

    void TriggerLandingImpact()
    {
        // Calculate how hard we hit the ground based on our vertical velocity right before landing
        float fallSeverity = Mathf.Clamp01(Mathf.Abs(playerController.VerticalVelocity) / 20f);

        // Push the camera offset down instantly
        currentLandingOffset = -landingDipAmount * fallSeverity;
    }

    void HandleMotionBlur()
    {
        if (speedBlurVolume != null)
        {
            // Calculate a 0 to 1 value based on how close we are to Dash Speed
            float speedRatio = (playerController.CurrentSpeed - playerController.RunSpeed) / (maxBlurSpeed - playerController.RunSpeed);

            // Fade the volume's weight in and out
            speedBlurVolume.weight = Mathf.Clamp01(speedRatio);
        }
    }
}
