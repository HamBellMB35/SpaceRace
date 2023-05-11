using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyForwardMove : MonoBehaviour
{
    
 public float forwardSpeed;

    private void Update()
    {
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);
    }
}
