using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDestroyer : MonoBehaviour
{

    public LayerMask ignoredLayer;
    private void OnTriggerEnter(Collider other)
    {
        //  if ((ignoredLayer.value & (1 << other.gameObject.layer)) != 0)
        // {
        //     return; 
        // }

        Debug.Log("Destroying Object");
        Destroy(other.gameObject);
    }
}
