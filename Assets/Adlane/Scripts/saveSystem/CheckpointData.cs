using UnityEngine;

[System.Serializable]
public class CheckpointData
{
    public int checkpointIndex;

    // Player transform
    public Vector3 position;
    public Quaternion rotation;

    // Physics state
    public Vector3 gravityDirection;
    public float verticalVelocity;
    public Vector3 currentVelocity;

    // Momentum meter
    public float momentumMeter;
}