using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{  
     public float minX;
    public float maxX;
    public float minY;
    public float maxY;
    public float ManuveringSpeed;
    public float waitTime = 2f;
    public float forwardSpeed;
    
    private float targetX;
    private float targetY;
    private bool isMoving = true;
    private float timer = 0f;
    private float initialWaitTimer;

    void Start()
    {
        // Set initial random target position
        CalculateNext();

        // Generate a pseudo-random value for the initial wait timer
        initialWaitTimer = Random.Range(0f, waitTime);
    }

    void FixedUpdate()
    {
        MoveEnemy();
    }

    private void MoveEnemy()
    {     
        if (isMoving)
        {
            // Move towards the target position in x and y axis
            Vector3 currentPosition = transform.position;
            Vector3 targetPosition = new Vector3(targetX, targetY, currentPosition.z);
            transform.position = Vector3.Lerp(currentPosition, targetPosition, ManuveringSpeed * Time.deltaTime);

            // If we've reached the target position, start the wait timer
            if (Vector3.Distance(currentPosition, targetPosition) < 0.1f)
            {
                isMoving = false;
            }
        }
        else
        {
            // Wait for the specified time before setting a new target position
            timer += Time.deltaTime;

            if (timer >= waitTime)
            {
                timer = 0f;
                isMoving = true;
                CalculateNext();
            }
        }

        // Move the object forward in the z-axis
         transform.position += Vector3.forward * forwardSpeed * Time.deltaTime;
    }   


    private void CalculateNext()
    {
        targetX = Random.Range(minX, maxX);
        targetY = Random.Range(minY, maxY);
    }
}
