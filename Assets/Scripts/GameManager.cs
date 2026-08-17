using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// The central hub for game state - this is the thing everything else in
// the game talks to when it needs to know or change "how's the run going."
// It tracks score and laser ammo, keeps the two UI text elements in sync
// with those numbers, and handles swapping between the gameplay UI and the
// game-over screen.
//
// Notice this uses a static singleton (gmInstance) instead of, say, every
// script holding its own reference dragged in via the Inspector. That's a
// deliberate (if old-school) pattern here: since there's only ever one
// GameManager in the scene, anything anywhere can just call
// GameManager.gmInstance.WhateverMethod() without needing a wired-up
// reference. The tradeoff is it's a bit of a "God object" that everything
// depends on globally - fine for a project this size, but worth knowing
// the pattern by name if this comes up in an interview.
public class GameManager : MonoBehaviour
{
    public static GameManager gmInstance;

    [Header("Gameplay Objects")]
    public GameObject player;
    public GameObject levelManager;

    [Header("UI Panels")]
    public GameObject laserCountUI;
    public GameObject scoreUI;
    public GameObject finalScoreUI;

    [Header("UI Text")]
    public Text scoreText;
    public Text finalScoreText;
    public Text laserCountText;

    public int laserCount = 30;

    private int score;

    private void Awake()
    {
        // Classic singleton setup: if nobody's claimed the gmInstance slot
        // yet, this becomes it. We don't handle the "what if a second
        // GameManager exists" case here (normally you'd Destroy the
        // duplicate) since this project only ever has one in the scene -
        // just flagging that's a corner we're deliberately not covering.
        if (gmInstance == null)
        {
            gmInstance = this;
        }
    }

    /// <summary>Ends the current run: hides gameplay UI and shows the final score screen.</summary>
    public void EndGame()
    {
        DisableGameplayUI();
        EnableFinalScoreUI();
    }

    /// <summary>Loads the main gameplay scene.</summary>
    public void LoadGame()
    {
        SceneManager.LoadScene(0);
    }

    public void UpdateScore()
    {
        score += 100;
        scoreText.text = "Score = " + score;
        finalScoreText.text = "Final Score = " + score;
    }

    /// <summary>Called every time the player fires a laser - spends one shot of ammo.</summary>
    public void UpdateLaserCount()
    {
        if (laserCount <= 0)
        {
            laserCount = 0;
            return;
        }

        laserCount--;
        laserCountText.text = "Laser = " + laserCount;
    }

    /// <summary>
    /// The opposite of UpdateLaserCount above - grants ammo instead of
    /// spending it. This is what a pickup (like a barrier the player flies
    /// through) calls to hand out bonus lasers. Takes an amount rather than
    /// always adding a fixed number, so different pickups can be worth
    /// different amounts just by setting a value in the Inspector, without
    /// ever needing to touch this method again.
    /// </summary>
    public void AddLasers(int amount)
    {
        laserCount += amount;
        laserCountText.text = "Laser = " + laserCount;
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