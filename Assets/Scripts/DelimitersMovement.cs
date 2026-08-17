using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Scrolls a lane-delimiter object toward the camera at a constant speed.</summary>
public class DelimitersMovement : MonoBehaviour
{
    [FormerlySerializedAs("delemeterSpeed")]
    public float delimiterSpeed;

    void Update()
    {
        transform.Translate(Vector3.forward * -delimiterSpeed * Time.deltaTime);
    }
}
