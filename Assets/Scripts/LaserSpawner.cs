using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereShooter : MonoBehaviour
{ 
    public GameObject spherePrefab;
    public float shootForce = 10f;
    public Transform shootTarget;
    public Transform spawnPoint;
    public AudioClip laserSound;
    public AudioClip noAmmoSound;


    public int sphereCount;

    void Start()
    {
        sphereCount = GetComponent<SphereShooter>().sphereCount;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) )
        {

            
            if (sphereCount <= 0)
            {
               AudioSource.PlayClipAtPoint(noAmmoSound, transform.position);
               return;
            }

            else
            {
                sphereCount -= 2;
                Debug.Log("*** SHOTS LEFT: " + sphereCount); 
                                                        
                ShootSphere(shootTarget);                           // Shoot the sphere towards the shoot target
                GameManager.gmInstance.UpdateLaserCount();
            }
        }
        

        
    }

    void ShootSphere(Transform target)
    {
      
        AudioSource.PlayClipAtPoint(laserSound, transform.position);
       
        GameObject sphere = Instantiate(spherePrefab, 
        spawnPoint.position, Quaternion.identity);              // Create a sphere at the spawn 
                                                                // point and shoot it towards the target

        sphere.GetComponent<Rigidbody>().AddForce(
            (target.position - spawnPoint.position).normalized  // Add Force to projectile
             * shootForce, ForceMode.Impulse);
        
       
        StartCoroutine(DestroySphere(sphere, 1f));               // Destroy the sphere after 1 second               
    }

    IEnumerator DestroySphere(GameObject sphere, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(sphere);
    }
}


