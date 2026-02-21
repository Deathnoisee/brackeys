using UnityEngine;
using System.Collections;   

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private FPController fpController;
    [SerializeField] private MomentumMeter momentumMeter;

    [Header("Settings")]

    private CheckpointData lastCheckpoint = null;
    private bool hasCheckpoint = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnValidate()
    {
        if (fpController == null)
            fpController = FindFirstObjectByType<FPController>();
        if (momentumMeter == null)
            momentumMeter = FindFirstObjectByType<MomentumMeter>();
    }

    public void SaveCheckpoint(int index, GameObject player)
    {
        // Only save if this is a newer checkpoint
        if (hasCheckpoint && lastCheckpoint.checkpointIndex >= index) return;

        lastCheckpoint = new CheckpointData
        {
            checkpointIndex = index,
            position = player.transform.position,
            rotation = player.transform.rotation,
            gravityDirection = fpController.GravityDirection,
            verticalVelocity = fpController.VerticalVelocity,
            currentVelocity = fpController.CurrentVelocity,
            momentumMeter = momentumMeter.GetCurrentMeter()
        };

        hasCheckpoint = true;
        Debug.Log($"Checkpoint {index} saved at {lastCheckpoint.position}");
    }

    public void Respawn()
    {
        if (!hasCheckpoint)
        {
            Debug.Log("No checkpoint saved yet!");
            return;
        }

        // Disable controller briefly to teleport cleanly
        fpController.GetComponent<CharacterController>().enabled = false;

        // Restore position and rotation
        fpController.transform.position = lastCheckpoint.position;
        fpController.transform.rotation = lastCheckpoint.rotation;

        fpController.GetComponent<CharacterController>().enabled = true;

        // Restore physics state
        fpController.SetGravityDirection(lastCheckpoint.gravityDirection);
        fpController.VerticalVelocity = 0f;    // Always reset velocity on respawn
        fpController.CurrentVelocity = Vector3.zero;

        // Restore momentum meter
        momentumMeter.SetMeter(lastCheckpoint.momentumMeter);

        Time.timeScale = 1f; // Ensure time is running after respawn

        Debug.Log($"Respawned at checkpoint {lastCheckpoint.checkpointIndex}");
    }


}
