using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Moves an enemy to random points within a bounding box while advancing forward.</summary>
/// <remarks>
/// CHANGED for object pooling: Start() became OnEnable(), and isMoving/
/// timer are now explicitly reset there too. This script doesn't call
/// Instantiate()/Destroy() itself, but it lives on the enemy prefab that
/// Randomizer.cs now spawns through ObjectPoolManager - meaning a given
/// enemy instance gets reused over and over rather than freshly created
/// each time. Start() only ever runs once per object, so it can't be
/// trusted to reset anything on reuse. Field initializers (like
/// "isMoving = true" below) have the exact same problem for a different
/// reason: they only run once too, at the object's original construction,
/// not again each time a pooled instance gets reactivated - so a reused
/// enemy that happened to be mid-"waiting" when it was last released would
/// otherwise come back already stuck waiting, instead of moving toward a
/// fresh target like a brand new enemy would.
/// </remarks>
public class Enemy : MonoBehaviour
{
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    [FormerlySerializedAs("ManuveringSpeed")]
    public float maneuveringSpeed;

    public float waitTime = 2f;
    public float forwardSpeed;

    private float targetX;
    private float targetY;
    private bool isMoving = true;
    private float timer;
    private float initialWaitTimer;

    private void OnEnable()
    {
        // Explicitly reset the movement state fields - see the class
        // comment above for why this can't be left to field initializers
        // once this object is a pooled, reused instance.
        isMoving = true;
        timer = 0f;

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
            transform.position = Vector3.Lerp(currentPosition, targetPosition, maneuveringSpeed * Time.deltaTime);

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