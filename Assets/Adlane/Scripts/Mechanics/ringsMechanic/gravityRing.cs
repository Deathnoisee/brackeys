using UnityEngine;
using System.Collections;

public class gravityRing : MonoBehaviour
{
    [SerializeField] private float bufferDuration = 2.5f;
    [SerializeField] private float slowMotionScale = 0.05f;
    [SerializeField] private float inputGracePeriod = 0.15f;

    private bool isBufferActive = false;
    private bool isGracePeriodOver = false;
    private Coroutine bufferCoroutine;
    private FPController fpController;

    // Store input snapshot when grace period ends — not before
    private Vector2 inputSnapshot = Vector2.zero;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            fpController = other.GetComponent<FPController>();

            if (fpController != null)
                bufferCoroutine = StartCoroutine(ActivateGravityBuffer());
        }
    }

    private void Update()
    {
        if (!isBufferActive || !isGracePeriodOver || fpController == null) return;

        // Read FRESH input after grace period — not cached input from before
        Vector2 input = fpController.MoveInput;

        if (input.y > 0.5f) EndBufferAndSetGravity(Vector3.up);
        else if (input.y < -0.5f) EndBufferAndSetGravity(Vector3.down);
    }

    private void EndBufferAndSetGravity(Vector3 gravityDirection)
    {
        if (!isBufferActive) return;

        isBufferActive = false;
        isGracePeriodOver = false;

        if (bufferCoroutine != null)
            StopCoroutine(bufferCoroutine);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        fpController.SetGravityDirection(gravityDirection);

        Destroy(gameObject);
    }

    private IEnumerator ActivateGravityBuffer()
    {
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        isBufferActive = true;
        isGracePeriodOver = false;

        // Wait for grace period in REAL time
        yield return new WaitForSecondsRealtime(inputGracePeriod);

        // Only start reading input AFTER the player has released their keys
        // Wait until input is neutral before enabling direction choice
        float waitedTime = 0f;
        float maxWait = 0.5f;
        while (fpController.MoveInput.sqrMagnitude > 0.1f && waitedTime < maxWait)
        {
            waitedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        isGracePeriodOver = true;

        // Wait remaining buffer time in real time
        float remaining = bufferDuration - inputGracePeriod - waitedTime;
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        // No input chosen — keep current gravity
        if (isBufferActive)
            EndBufferAndSetGravity(fpController.GravityDirection);
    }
}
