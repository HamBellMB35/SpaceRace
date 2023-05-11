using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine("Destruct");
    }

    private IEnumerator Destruct()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
