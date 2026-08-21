using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Spawns a random obstacle chunk when this object becomes active, then spawns enemy ships on a timer.</summary>
/// <remarks>
/// CHANGED for object pooling, in two ways:
///
/// 1) Instantiate() calls became ObjectPoolManager.Spawn() calls, same as
///    everywhere else in this pass.
///
/// 2) Start() became OnEnable(). This one's important, not just stylistic:
///    this script lives on the map CHUNK prefab itself, which is now a
///    pooled object that gets reused over and over instead of freshly
///    Instantiate()d every time. Start() only ever runs ONCE per object,
///    the very first time it's created - so if this had stayed as Start(),
///    a REUSED chunk (the 2nd, 3rd, 4th... time this same instance gets
///    pulled out of the pool) would never spawn a fresh obstacle or start
///    the enemy-spawn loop again, since Start() already fired and used up
///    once, back on its very first spawn. OnEnable() fixes that by firing
///    every single time this object gets reactivated, whether that's its
///    first-ever spawn or its fiftieth reuse - which is exactly the "reset
///    yourself, you're back in play" moment this logic actually needs.
/// </remarks>
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

    private void OnEnable()
    {
        RandomizeMapChunk();
        StartCoroutine(SpawnEnemies());
    }

    private void RandomizeMapChunk()
    {
        obstacleIndex = (byte)Random.Range(0, obstacles.Length);

        ObjectPoolManager.instance.Spawn(obstacles[obstacleIndex],
            obstaclesSpawnPoints[obstacleIndex].transform.position,
            Quaternion.identity);
    }

    private void SpawnShips()
    {
        enemyIndex = (byte)Random.Range(0, enemies.Length);

        ObjectPoolManager.instance.Spawn(enemies[enemyIndex],
            enemiesSpawnPoints[enemyIndex].transform.position,
            Quaternion.identity);
    }

    // Runs for as long as this chunk stays active. No explicit "stop" is
    // needed when the chunk is eventually released back to its pool -
    // deactivating a GameObject automatically stops every coroutine
    // running on it, so this loop simply halts on its own the instant the
    // chunk goes inactive, and starts fresh again (via OnEnable() above)
    // the next time this same instance gets reused.
    private IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(shipSpawnDelay);
            SpawnShips();
        }
    }
}