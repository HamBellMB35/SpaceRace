using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GryoMovement : MonoBehaviour
{

    private bool gyroEnabled;
    private Gyroscope gyro;

    void Start()
    {
         gyroEnabled = EnableGyro(true);
    }

  
    void Update()
    {
         if (gyroEnabled)
        {
            transform.Translate(-gyro.rotationRateUnbiased.x, -gyro.rotationRateUnbiased.y, 0);
        }
    }

       private bool EnableGyro(bool boolValue)
    {
        if (SystemInfo.supportsGyroscope)
        {
            gyro = Input.gyro;
            gyro.enabled = boolValue;
            
        }
        return boolValue;
    }
}
