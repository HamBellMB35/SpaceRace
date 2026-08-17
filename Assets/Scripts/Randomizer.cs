using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Spawns a random obstacle chunk at start, then spawns enemy ships on a timer.</summary>
public class Randomizer : MonoBehaviour
{
    public GameObject[] obstacles;
    public GameObject[] obstaclesSpawnPoints;
    public GameObject[] enemies;
    public GameObject[] enemiesSpawnPoints;

    [FormerlySerializedAs("ShipSapawnDelay")]
    public float shipSpawnDelay;

    private byte obstacleIndex;
    private byte enemyIndex;

    private void Start()
    {
        RandomizeMapChunk();
        StartCoroutine(SpawnEnemies());
    }

    private void RandomizeMapChunk()
    {
        obstacleIndex = (byte)Random.Range(0, obstacles.Length);

        Instantiate(obstacles[obstacleIndex],
            obstaclesSpawnPoints[obstacleIndex].transform.position,
            Quaternion.identity);
    }

    private void SpawnShips()
    {
        enemyIndex = (byte)Random.Range(0, enemies.Length);

        Instantiate(enemies[enemyIndex],
            enemiesSpawnPoints[enemyIndex].transform.position,
            Quaternion.identity);
    }

    private IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(shipSpawnDelay);
            SpawnShips();
        }
    }
}
