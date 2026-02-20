using UnityEngine;
using System;

public class NoFrictionAoe : MonoBehaviour
{
    [SerializeField] private float speed = 10f;

    private Vector3 direction;
    private bool hasLanded = false;
    public Action onLanded;

    public void Launch(Vector3 dir)
    {
        direction = dir.normalized;
    }

    private void Update()
    {
        if (hasLanded) return;

        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;

        hasLanded = true;
        direction = Vector3.zero;

        // freeze in place
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        onLanded?.Invoke();
    }
}
