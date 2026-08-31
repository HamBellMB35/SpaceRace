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

    // NEW: stops obstacles/enemies from spawning right on top of the
    // player - most noticeably right at the very start of a run, before
    // the player's had any chance to react to something that was never
    // fairly "approaching" from a distance in the first place. This is
    // checked fresh every time something's ABOUT to spawn, rather than
    // only being some special "game start only" logic - which turns out
    // to be all it needs: once a run gets going, SpaceChunksGenerator
    // always creates new chunks (and everything inside them) hundreds of
    // units AHEAD of wherever the player currently is, so this check
    // naturally never has anything to actually do after the opening
    // moments - it just quietly stops mattering on its own.
    [Tooltip("Nothing will spawn closer than this many units to the player's CURRENT position. Mainly matters at the very start of a run; later spawns are already generated far ahead of the player anyway, so this rarely does anything past the opening few seconds.")]
    public float minSpawnDistanceFromPlayer = 30f;

    // Cached ONCE and shared by every Randomizer instance/reuse, rather
    // than each pooled chunk re-running GameObject.FindGameObjectWithTag
    // on every single activation - there's only ever one Player in this
    // game, and its Transform never changes, so there's no reason to pay
    // that lookup cost more than once for the whole run.
    private static Transform cachedPlayerTransform;

    private byte obstacleIndex;
    private byte enemyIndex;

    private void OnEnable()
    {
        RandomizeMapChunk();
        StartCoroutine(SpawnEnemies());
    }

    // Finds and caches the player's Transform the first time anything
    // actually needs it, then just hands back the cached value on every
    // later call. Using the "Player" tag here (rather than a public
    // Inspector field) means this works automatically on every chunk
    // prefab without needing to manually drag a Player reference into
    // each one by hand.
    private static Transform GetPlayerTransform()
    {
        if (cachedPlayerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                cachedPlayerTransform = playerObject.transform;
            }
        }

        return cachedPlayerTransform;
    }

    // Shared by both spawn methods below - true means "too close, don't
    // spawn here right now." If the player's Transform can't be found for
    // some reason, this deliberately falls back to false (allow the
    // spawn) rather than silently blocking every single spawn in the game
    // forever just because one lookup failed.
    private bool IsTooCloseToPlayer(Vector3 spawnPosition)
    {
        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            return false;
        }

        return Vector3.Distance(spawnPosition, playerTransform.position) < minSpawnDistanceFromPlayer;
    }

    private void RandomizeMapChunk()
    {
        obstacleIndex = (byte)Random.Range(0, obstacles.Length);

        Vector3 spawnPosition = obstaclesSpawnPoints[obstacleIndex].transform.position;

        if (IsTooCloseToPlayer(spawnPosition))
        {
            // Deliberately just skip this one spawn rather than trying to
            // pick a different spawn point - obstacles and their spawn
            // points are paired together by index (see the class fields
            // above), so "too close" here just means this particular
            // chunk activation goes without this particular obstacle,
            // which is a perfectly fine outcome for the rare early case
            // this actually triggers.
            return;
        }

        ObjectPoolManager.instance.Spawn(obstacles[obstacleIndex],
            spawnPosition,
            Quaternion.identity);
    }

    private void SpawnShips()
    {
        enemyIndex = (byte)Random.Range(0, enemies.Length);

        Vector3 spawnPosition = enemiesSpawnPoints[enemyIndex].transform.position;

        if (IsTooCloseToPlayer(spawnPosition))
        {
            return;
        }

        ObjectPoolManager.instance.Spawn(enemies[enemyIndex],
            spawnPosition,
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