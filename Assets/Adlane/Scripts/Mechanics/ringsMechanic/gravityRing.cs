using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class gravityRing : MonoBehaviour
{
    [SerializeField] private float bufferDuration = 0.5f;
    [SerializeField] private float slowMotionScale = 0.1f;
    [SerializeField] private float gravityForce = 9.8f;

    private bool isBufferActive = false;
    private Transform playerTransform;
    private Coroutine bufferCoroutine;
    private FPPlayer fpPlayer;
    private FPController fpController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            fpPlayer = other.GetComponent<FPPlayer>();
            fpController = other.GetComponent<FPController>();

            if (fpPlayer != null)
                //    fpPlayer.OnMoveInput += HandleDirectionInput;

                bufferCoroutine = StartCoroutine(ActivateGravityBuffer());
        }
    }

    private void HandleDirectionInput(Vector2 input)
    {
        if (!isBufferActive) return;

        if (input.y > 0.5f)      EndBufferAndSetGravity(Vector3.up);
        else if (input.y < -0.5f) EndBufferAndSetGravity(Vector3.down);
        else if (input.x < -0.5f) EndBufferAndSetGravity(Vector3.left);
        else if (input.x > 0.5f)  EndBufferAndSetGravity(Vector3.right);
    }

    private void EndBufferAndSetGravity(Vector3 gravityDirection)
    {
        if (!isBufferActive) return;

        isBufferActive = false;

        if (bufferCoroutine != null)
            StopCoroutine(bufferCoroutine);

        if (fpPlayer != null)
            // fpPlayer.OnMoveInput -= HandleDirectionInput;

            // Restore time
            Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // Apply the new gravity direction
        if (fpController != null)
            // fpController.SetGravityDirection(gravityDirection * gravityForce);

            Destroy(gameObject);
    }

    private IEnumerator ActivateGravityBuffer()
    {
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        isBufferActive = true;

        yield return new WaitForSecondsRealtime(bufferDuration);

        if (isBufferActive)
            EndBufferAndSetGravity(Vector3.down);
    }
}
