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

        Instantiate(spaceChunks[chunkIndex], new Vector3(0, 0, zPosition), Quaternion.identity);

        zPosition += 400;

        yield return new WaitForSeconds(waitTime);

        creatingMapChunk = false;
    }
}
