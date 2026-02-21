using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int checkpointIndex;
    private bool isActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated) return;
        if (!other.CompareTag("Player")) return;

        isActivated = true;
        CheckpointManager.Instance.SaveCheckpoint(checkpointIndex, other.gameObject);
    }
}
