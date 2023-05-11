using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelimitersMovement : MonoBehaviour
{
    public float delemeterSpeed;
    void Update()
    {
         transform.Translate(Vector3.forward * - delemeterSpeed * Time.deltaTime);
    }
}
