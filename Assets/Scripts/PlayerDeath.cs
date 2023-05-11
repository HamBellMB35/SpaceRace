using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Obstacle")
        {
           RestartGame();
        }
       
    }

    public void RestartGame() {GameManager.gmInstance.Restart();}
    
        
    
}
