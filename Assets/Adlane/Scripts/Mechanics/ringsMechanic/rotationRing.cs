using UnityEngine;
using System.Collections;

public class rotationRing : MonoBehaviour
{
    [SerializeField] private float bufferDuration = 0.5f;
    [SerializeField] private float slowMotionScale = 0.1f;
    [SerializeField] private float rotationDuration = 0.2f;
    [SerializeField] private float inputGracePeriod = 0.1f;
    [SerializeField] private float launchSpeed = 20f; // How fast to throw the player

    private bool isBufferActive = false;
    private bool isGracePeriodOver = false;
    private Transform playerTransform;
    private Coroutine bufferCoroutine;
    private FPController fpController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            fpController = other.GetComponent<FPController>();

            if (fpController != null)
                bufferCoroutine = StartCoroutine(ActivateRotationBuffer());
        }
    }

    private void Update()
    {
        if (!isBufferActive || !isGracePeriodOver || fpController == null) return;

        Vector2 input = fpController.MoveInput;

        if (input.x > 0.5f) EndBufferAndRotate(90f);
        else if (input.x < -0.5f) EndBufferAndRotate(-90f);
        else if (input.y < -0.5f) EndBufferAndRotate(180f);
        else if (input.y > 0.5f) EndBufferAndRotate(0f);
    }

    private void EndBufferAndRotate(float angle)
    {
        if (!isBufferActive) return;

        isBufferActive = false;
        isGracePeriodOver = false;

        if (bufferCoroutine != null)
            StopCoroutine(bufferCoroutine);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        StartCoroutine(SmoothRotate(angle));
    }

    private IEnumerator SmoothRotate(float angle)
    {
        Quaternion startRotation = playerTransform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, angle, 0f);

        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            t = t * t * (3f - 2f * t);

            playerTransform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
            yield return null;
        }

        playerTransform.rotation = targetRotation;

        // Launch player in the new forward direction after rotation completes
        Vector3 launchDir = Vector3.ProjectOnPlane(playerTransform.forward, fpController.GravityDirection).normalized;
        fpController.CurrentVelocity = launchDir * launchSpeed;

        Destroy(gameObject);
    }

    private IEnumerator ActivateRotationBuffer()
    {
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        isBufferActive = true;
        isGracePeriodOver = false;

        // Wait grace period before reading input
        yield return new WaitForSecondsRealtime(inputGracePeriod);

        isGracePeriodOver = true;

        // Wait remaining buffer time
        yield return new WaitForSecondsRealtime(bufferDuration - inputGracePeriod);

        // No input — default keep forward
        if (isBufferActive)
            EndBufferAndRotate(0f);
    }
}
