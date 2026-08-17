using System.Collections;
using UnityEngine;

/// <summary>Destroys this object one second after it spawns.</summary>
public class SelfDestruct : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(Destruct());
    }

    private IEnumerator Destruct()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
