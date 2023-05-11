//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceChunksGenerator : MonoBehaviour
{
    public GameObject[] spaceChunks;
    public int zPosition = 200;
    public int chunkIndex;
    public float waitTime;
    public bool creatingMapChunk = false;
  
    void Update()
    {           

        if(creatingMapChunk == false)
        {
            creatingMapChunk = true;
            StartCoroutine(CreateMapChunk());
        }
    }

   IEnumerator CreateMapChunk()
   {
        chunkIndex = Random.Range(0,spaceChunks.Length);
       
        Instantiate(spaceChunks[chunkIndex], 
        new Vector3(0,0,zPosition),
        Quaternion.identity );
       
        zPosition += 400;
       
        yield return new WaitForSeconds(waitTime);
        
        creatingMapChunk = false;
   }
}
