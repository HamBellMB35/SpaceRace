//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Randomizer : MonoBehaviour
{
    public GameObject[] obstacles;
    public GameObject[] obstaclesSpawnPoints;
    public GameObject[] enemies;
    public GameObject[] enemiesSpawnPoints;

    public float ShipSapawnDelay;
    private byte obstacleIndex;
    private byte enemyIndex;

    
  
    private void Start()
    {
        RandomizeMapChunk();
        StartCoroutine("SpawnEnemies");

        
    }
    void Update()
    {
        
    }

    private void RandomizeMapChunk()
    {
       obstacleIndex = (byte) Random.Range(0,obstacles.Length);
      

        Instantiate(obstacles[obstacleIndex], 
        obstaclesSpawnPoints[obstacleIndex].transform.position,
        Quaternion.identity );
    }

    private void SpawnShips()
    {
        enemyIndex = (byte) Random.Range(0,enemies.Length);

       Instantiate(enemies[enemyIndex], 
        enemiesSpawnPoints[enemyIndex].transform.position,
        Quaternion.identity );
    }

    private IEnumerator SpawnEnemies()
    {
        while(true)
        {
            yield return new WaitForSeconds(ShipSapawnDelay);
            SpawnShips();
        }
        
    }
}
