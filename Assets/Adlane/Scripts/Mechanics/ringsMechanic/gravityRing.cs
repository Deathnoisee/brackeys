using UnityEngine;
using System.Collections;

public class gravityRing : MonoBehaviour
{
    [SerializeField] private float bufferDuration = 0.5f;
    [SerializeField] private float slowMotionScale = 0.1f;
    [SerializeField] private float inputGracePeriod = 0.1f;

    private bool isBufferActive = false;
    private bool isGracePeriodOver = false;
    private Coroutine bufferCoroutine;
    private FPController fpController;

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

        Vector2 input = fpController.MoveInput;

        if (input.y > 0.5f) EndBufferAndSetGravity(Vector3.up);
        else if (input.y < -0.5f) EndBufferAndSetGravity(Vector3.down);
        else if (input.x < -0.5f) EndBufferAndSetGravity(-fpController.transform.right);
        else if (input.x > 0.5f) EndBufferAndSetGravity(fpController.transform.right);
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

        // Grace period — ignore input so jump/movement keys don't accidentally trigger
        yield return new WaitForSecondsRealtime(inputGracePeriod);

        isGracePeriodOver = true;

        // Wait remaining buffer time
        yield return new WaitForSecondsRealtime(bufferDuration - inputGracePeriod);

        // No input chosen — default back to down
        if (isBufferActive)
            EndBufferAndSetGravity(Vector3.down);
    }
}
