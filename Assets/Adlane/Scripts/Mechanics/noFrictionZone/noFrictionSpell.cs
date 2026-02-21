using UnityEngine;
using UnityEngine.InputSystem;

public class noFrictionSpell : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float cd = 20f;
    [SerializeField] private float duration = 5f;
    [Header("References")]
    [SerializeField] private GameObject aoePrefab;

    private GameObject currentAoe;
    private float lastCastTime = -Mathf.Infinity;

    public void CastSpell(InputAction.CallbackContext context)
    {
        if (context.performed && currentAoe == null && Time.time >= lastCastTime + cd)
        {
            ThrowProjectile();
        }
    }

    private void ThrowProjectile()
    {
        lastCastTime = Time.time;

        Vector3 spawnPos = transform.position + transform.forward;
        currentAoe = Instantiate(aoePrefab, spawnPos, Quaternion.identity);

         NoFrictionAoe projectile = currentAoe.GetComponent<NoFrictionAoe>();
        if (projectile != null)
        {
            projectile.Launch(transform.forward);
            projectile.onLanded += () => Invoke(nameof(DestroyAoe), duration);
        }
    }

    private void DestroyAoe()
    {
        if (currentAoe != null)
        {
            Destroy(currentAoe);
            currentAoe = null;
        }
    }
}
