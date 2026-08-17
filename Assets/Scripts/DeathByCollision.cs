using System.Collections;
using UnityEngine;

/// <summary>Destroys this object and spawns an explosion when it hits anything but the barrier.</summary>
public class DeathByCollision : MonoBehaviour
{
    public GameObject explosionPrefab;
    private GameObject explosion;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Barrier"))
        {
            explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
            StartCoroutine(DestroyExplosion());
        }
    }

    private IEnumerator DestroyExplosion()
    {
        yield return new WaitForSeconds(1f);
        Destroy(explosion);
    }
}
