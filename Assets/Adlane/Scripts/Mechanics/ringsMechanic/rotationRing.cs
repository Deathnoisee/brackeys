using UnityEngine;
using System.Collections;

public class rotationRing : MonoBehaviour
{
    [SerializeField] private float bufferDuration = 0.5f;
    [SerializeField] private float slowMotionScale = 0.1f;
    [SerializeField] private float rotationDuration = 0.2f;

    private bool isBufferActive = false;
    private Transform playerTransform;
    private Coroutine bufferCoroutine;
    private FPPlayer fpPlayer;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            fpPlayer = other.GetComponent<FPPlayer>();

            if (fpPlayer != null)
                //fpPlayer.OnMoveInput += HandleDirectionInput;

                bufferCoroutine = StartCoroutine(ActivateRotationBuffer());
        }
    }

    private void HandleDirectionInput(Vector2 input)
    {
        if (!isBufferActive) return;

        if (input.x > 0.5f)
            EndBufferAndRotate(90f);
        else if (input.x < -0.5f)
            EndBufferAndRotate(-90f);
        else if (input.y < -0.5f)
            EndBufferAndRotate(180f);
        else if (input.y > 0.5f)
            EndBufferAndRotate(0f);
    }

    private void EndBufferAndRotate(float angle)
    {
        if (!isBufferActive) return;

        isBufferActive = false;

        if (bufferCoroutine != null)
            StopCoroutine(bufferCoroutine);

        if (fpPlayer != null)
            //fpPlayer.OnMoveInput -= HandleDirectionInput; // Unsubscribe

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
        Destroy(gameObject);
    }

    private IEnumerator ActivateRotationBuffer()
    {
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        isBufferActive = true;

        yield return new WaitForSecondsRealtime(bufferDuration);

        if (isBufferActive)
            EndBufferAndRotate(0f); // Default keep forward
    }
}
