using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager gmInstance;
    public GameObject player;
    public GameObject levelManager;
    public GameObject laserCountUI;
    public GameObject scoreUI;
    public GameObject finalScoreUI;
    public Text scoreText;
    public Text finalScoreText;
    public Text laserCountText;
    private int score = 0;
    public int laserCount = 30;
    
    
    private void Awake()
    {
        if(gmInstance == null)
        {
            gmInstance = this;
        }
    }

    public void Restart()
    {
        DisableGameplayUI();
        EnableFinalScoreUI();
       // SceneManager.LoadScene(1);
       if(finalScoreUI && Input.anyKey)
       {
            
            SceneManager.LoadScene(1);
        }
        
    }

    public void LoadGame()
    {
        SceneManager.LoadScene(0);
    }

    public void UpdateScore()
    {
        score += 100;
        scoreText.text = "Score = " + score.ToString();
        finalScoreText.text = "Final Score = " + score.ToString();
    }

     public void UpdateLaserCount()
    {
        if(laserCount <= 0){laserCount = 0;}

        else
        {
            laserCount--;
            laserCountText.text = "Laser = " + laserCount.ToString();
        }
       
    }

    private void DisableGameplayUI()
    {
        levelManager.SetActive(false);
        player.SetActive(false);
        scoreUI.SetActive(false);
        laserCountUI.SetActive(false);
    }

    private void EnableFinalScoreUI()
    {
        finalScoreUI.SetActive(true);
       
    }
}
