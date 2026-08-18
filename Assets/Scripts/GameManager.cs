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
//
// One project-specific wrinkle worth explaining: this project has TWO
// scenes, each with its own GameManager object - one living quietly in the
// menu scene (UI_v2), whose only real job is to respond to the "New Game"
// button by loading the actual gameplay scene, and a second, fully wired
// one that lives in the gameplay scene itself (GamePlayScene) and does all
// the real score/ammo/UI work. The menu's copy is INTENTIONALLY left with
// its UI Text fields empty, since nothing on the menu screen ever needs
// them. That's exactly why the null-checks below live right next to where
// each field actually gets used, rather than all being checked eagerly in
// Awake() - checking eagerly used to mean the menu's harmless, by-design
// empty GameManager would print the same "not assigned!" warnings as a
// genuinely broken one in the gameplay scene, which was confusing and
// made a real problem indistinguishable from expected behavior.
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
        // If a second GameManager shows up in the SAME scene at the same
        // time, that's a genuine setup mistake (as opposed to the
        // menu-scene/gameplay-scene split described above, which is two
        // GameManagers in two DIFFERENT scenes, only one of which is ever
        // loaded at once - that's fine). This case really would mean two
        // scripts fighting over the same gmInstance slot, so it's still
        // worth catching loudly and cleaning up automatically.
        if (gmInstance != null && gmInstance != this)
        {
            Debug.LogError(
                $"[GameManager] Found a second GameManager ('{name}') in the scene - '{gmInstance.name}' already claimed gmInstance. " +
                $"Only the FIRST one's Inspector fields are actually in use, so if '{name}' is the one you configured, that mismatch is your bug. " +
                "Destroying this duplicate so the game keeps running, but you should go delete the extra GameManager object from the Hierarchy.",
                this);
            Destroy(gameObject);
            return;
        }

        gmInstance = this;

        // Deliberately NOT checking scoreText/finalScoreText/laserCountText
        // here anymore - see the class comment above for why. Those checks
        // now live inline in UpdateScore(), UpdateLaserCount(), and
        // AddLasers(), right where each field is actually used, so a
        // GameManager that's never asked to update the UI (like the
        // menu's) never complains about UI fields it was never given.
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

        // Same "warn once, skip gracefully" pattern in both branches below:
        // the actual score number always increments correctly above,
        // regardless of whether either Text field is hooked up - only the
        // on-screen DISPLAY of it silently skips if a field's missing,
        // rather than throwing and potentially interrupting whatever
        // triggered this (a collision, a trigger volume, etc.).
        if (scoreText == null)
        {
            Debug.LogWarning($"[GameManager] 'scoreText' isn't assigned on '{name}' - score is still tracked internally, just not shown on screen.", this);
        }
        else
        {
            scoreText.text = "Score = " + score;
        }

        if (finalScoreText == null)
        {
            Debug.LogWarning($"[GameManager] 'finalScoreText' isn't assigned on '{name}' - the game-over screen won't show a number until this is wired up.", this);
        }
        else
        {
            finalScoreText.text = "Final Score = " + score;
        }
    }

    /// <summary>Called every time the player fires a laser - spends one shot of ammo.</summary>
    public void UpdateLaserCount()
    {
        if (laserCount <= 0)
        {
            laserCount = 0;
            return;
        }

        // The actual ammo count always spends correctly, whether or not
        // the on-screen text is wired up - firing shouldn't stop working
        // just because a UI label is missing.
        laserCount--;

        if (laserCountText == null)
        {
            Debug.LogWarning($"[GameManager] 'laserCountText' isn't assigned on '{name}' - ammo is still being spent correctly, it just isn't shown on screen.", this);
            return;
        }

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

        if (laserCountText == null)
        {
            Debug.LogWarning($"[GameManager] 'laserCountText' isn't assigned on '{name}' - the bonus ammo was still granted, it just isn't shown on screen.", this);
            return;
        }

        laserCountText.text = "Laser = " + laserCount;
    }

    private void DisableGameplayUI()
    {
        // Each of these is checked individually rather than assuming all
        // five gameplay-object references are present - this way, one
        // missing reference (say, laserCountUI) doesn't stop the other
        // four from correctly hiding themselves, and you get a specific
        // pointer toward exactly which one still needs wiring instead of a
        // single crash that tells you nothing about which field was at fault.
        WarnAndSkipIfMissing(levelManager, nameof(levelManager), obj => obj.SetActive(false));
        WarnAndSkipIfMissing(player, nameof(player), obj => obj.SetActive(false));
        WarnAndSkipIfMissing(scoreUI, nameof(scoreUI), obj => obj.SetActive(false));
        WarnAndSkipIfMissing(laserCountUI, nameof(laserCountUI), obj => obj.SetActive(false));
    }

    private void EnableFinalScoreUI()
    {
        WarnAndSkipIfMissing(finalScoreUI, nameof(finalScoreUI), obj => obj.SetActive(true));
    }

    // Small shared helper for the two methods above: if the given
    // GameObject reference is missing, log exactly which field is empty
    // and move on instead of throwing; otherwise run whatever action was
    // asked for (SetActive(true) or SetActive(false), in these two cases).
    // Using a System.Action here just avoids writing the same
    // "if null, warn, else do the thing" block out five separate times.
    private void WarnAndSkipIfMissing(GameObject fieldValue, string fieldName, System.Action<GameObject> action)
    {
        if (fieldValue == null)
        {
            Debug.LogWarning($"[GameManager] '{fieldName}' isn't assigned on '{name}' - skipping it.", this);
            return;
        }

        action(fieldValue);
    }
}