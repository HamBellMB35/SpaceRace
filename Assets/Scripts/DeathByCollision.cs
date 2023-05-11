using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathByCollision : MonoBehaviour
{
    public GameObject explosionPrefab;
    GameObject explosion;

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag != "Barrier")
        {
            explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
            StartCoroutine("DestroyExplosion");
        }
       
       
    }

    IEnumerator DestroyExplosion()
    {
        yield return new WaitForSeconds(1f);
        Destroy(explosion);
        
    }
}
