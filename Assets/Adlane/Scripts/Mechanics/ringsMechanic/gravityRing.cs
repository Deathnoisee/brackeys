using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GravityRing : MonoBehaviour
{
    [SerializeField] private float bufferDuration = 0.5f;

    private Vector3 currentGravity = Vector3.down;
    private bool inGravityRing = false;
    private bool gravitySelectionActive = false;

    private PlayerInput playerInput; 
    private CharacterController characterController; 

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        characterController = GetComponent<CharacterController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GravityRing"))
        {
            inGravityRing = true;
            gravitySelectionActive = true;

            StartCoroutine(GravitySelectionBuffer());
        }
    }

    private void Update()
    {
        if (gravitySelectionActive)
        {
            Vector2 input = playerInput.actions["Move"].ReadValue<Vector2>();

            if (input.y > 0) 
            {
                SetGravity(Vector3.up);
            }
            else if (input.y < 0)
            {
                SetGravity(Vector3.down);
            }
            else if (input.x > 0) 
            {
                SetGravity(Vector3.right);
            }
            else if (input.x < 0)
            {
                SetGravity(Vector3.left);
            }
        }

        if (!gravitySelectionActive)
        {
            //characterController.gameObject.GetComponent<FPController>().setGravity(currentGravity);
        }
    }

    private void SetGravity(Vector3 newGravity)
    {
        currentGravity = newGravity;
        gravitySelectionActive = false; 
    }

    private IEnumerator GravitySelectionBuffer()
    {
        yield return new WaitForSeconds(bufferDuration);


        if (gravitySelectionActive)
        {
            SetGravity(Vector3.down);
        }

        inGravityRing = false;
        Destroy(gameObject);
    }
}
