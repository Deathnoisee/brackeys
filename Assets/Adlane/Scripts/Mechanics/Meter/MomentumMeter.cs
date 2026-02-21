using UnityEngine;
using UnityEngine.UI;

public class MomentumMeter : MonoBehaviour
{   
    // velocity increases the meter so constant checking for velocity to increase the meter, 10 velocity = 1 point per sec, 100 velocity = 10 points, 
    [SerializeField] private float velocityToMeterRatio = 0.1f; 
    [SerializeField] private float maxMeterValue = 100f;
    [SerializeField] private float decayRate = 5f;
    [SerializeField] private Slider meterSlider; 
    private float currentMeter = 0f;
    private CharacterController characterController;


    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        float velocityMagnitude = characterController.velocity.magnitude;
        if (velocityMagnitude > 0f)
        {
        currentMeter += velocityMagnitude * velocityToMeterRatio * Time.deltaTime;
        } else if (velocityMagnitude == 0f && currentMeter > 0f)
        {
            currentMeter -= decayRate * Time.deltaTime;
        }
        currentMeter = Mathf.Clamp(currentMeter, 0f, maxMeterValue);
        if (meterSlider != null)
        {
            meterSlider.value = currentMeter / maxMeterValue;
        }
    }

    public float GetCurrentMeter()
    {
        return currentMeter;
    }
    public void ResetMeter()
    {
        currentMeter = 0f;
    }

    public void SetMeter(float value)
    {
        currentMeter = Mathf.Clamp(value, 0f, maxMeterValue);
    }

}
