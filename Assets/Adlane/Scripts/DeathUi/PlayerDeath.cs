using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.gameObject.GetComponent<Dying>().Die();
        }
    }
}
