using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{ public float sidewaysSpeed;
    public float verticalSpeed;
    public float forwardSpeed;
    private float xInput;
    private float yInput;
    private float maxX;
    public float minMovementX = -10f;
    public float maxMovementX = 10f;
    public float minMovementY = -10f;
    public float maxMovementY = 10f;
    
    private Rigidbody rb;
    Vector3 movement;
    public float forceDecay = 0.5f; // rate at which force decays
    public float maxZVelocity = 10f; // maximum velocity in the z direction
    public float rotationSpeed = 10f; // rotation speed of the object
    public float maxRotationAngleX = 45f; // maximum rotation angle for x and y axes
    public float maxRotationAngleY = 10f; // maximum rotation angle for x and y axes

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.visible = false;
    }

    void Update()
    {
        Move();
    }

    private void Move()
    {
       xInput = Input.GetAxis("Horizontal");
        yInput = Input.GetAxis("Vertical");

        // Clamp movement values
        float clampedX = Mathf.Clamp(transform.position.x + (xInput * sidewaysSpeed * Time.deltaTime), minMovementX, maxMovementX);
        float clampedY = Mathf.Clamp(transform.position.y + (yInput * verticalSpeed * Time.deltaTime), minMovementY, maxMovementY);
    
        movement = new Vector3(clampedX - transform.position.x, clampedY - transform.position.y, forwardSpeed * Time.deltaTime);
        rb.AddForce(movement, ForceMode.Impulse);
    
        float rotationAngleX = 0f;
        float rotationAngleY = 0f;
    
        if (yInput < 0f)
        {
            rotationAngleX = Mathf.Lerp(0f, maxRotationAngleY, -yInput);
        }
        else if (yInput > 0f)
        {
            rotationAngleX = Mathf.Lerp(0f, -maxRotationAngleY, yInput);
        }
        if (xInput < 0f)
        {
            rotationAngleY = Mathf.Lerp(0f, -maxRotationAngleX, -xInput);
        }
        else if (xInput > 0f)
        {
            rotationAngleY = Mathf.Lerp(0f, maxRotationAngleX, xInput);
        }
    
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(rotationAngleX, 0f, rotationAngleY), rotationSpeed * Time.deltaTime);
    
        rb.velocity = new Vector3(
            Mathf.Lerp(rb.velocity.x, 0f, forceDecay * Time.deltaTime),
            Mathf.Lerp(rb.velocity.y, 0f, forceDecay * Time.deltaTime),
            Mathf.Clamp(rb.velocity.z, -maxZVelocity, maxZVelocity)
        );
    }
}
