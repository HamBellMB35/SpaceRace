using System.Collections;
using UnityEngine;

/// <summary>Continuously spawns random space-chunk segments ahead of the player.</summary>
public class SpaceChunksGenerator : MonoBehaviour
{
    public GameObject[] spaceChunks;
    public int zPosition = 200;
    public int chunkIndex;
    public float waitTime;
    public bool creatingMapChunk;

    void Update()
    {
        if (!creatingMapChunk)
        {
            creatingMapChunk = true;
            StartCoroutine(CreateMapChunk());
        }
    }

    private IEnumerator CreateMapChunk()
    {
        chunkIndex = Random.Range(0, spaceChunks.Length);

        // CHANGED from Instantiate() to ObjectPoolManager - map chunks are
        // large, complex objects (with their own nested obstacles and
        // enemies), and a new one gets created every waitTime seconds for
        // the entire length of a run. That makes them the single biggest
        // GC win of anything being pooled in this pass.
        ObjectPoolManager.instance.Spawn(spaceChunks[chunkIndex], new Vector3(0, 0, zPosition), Quaternion.identity);

        zPosition += 400;

        yield return new WaitForSeconds(waitTime);

        creatingMapChunk = false;
    }
}