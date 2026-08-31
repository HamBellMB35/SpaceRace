using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

// The central COORDINATOR for game state - the thing everything else in
// the game still talks to when it needs to know or change "how's the run
// going." That part hasn't changed.
//
// What HAS changed: this used to also be the thing that did every bit of
// actual number-crunching itself - incrementing score, decrementing ammo,
// deciding when lives ran out - all mixed in with Unity-specific concerns
// like updating Text elements, showing/hiding UI panels, and holding the
// static singleton reference. That's the textbook definition of a "God
// object": one class doing several genuinely separate jobs at once, which
// makes it harder to test (you can't check "does losing a life work
// right" without a whole scene and Play Mode) and harder to reason about
// (every new feature's first instinct is "I'll just add it to
// GameManager," since that's where everything else already lives).
//
// The fix here is a common, low-risk pattern: keep GameManager as the
// single Unity-facing coordinator - same public methods, same Inspector
// fields, same static gmInstance access point, so NOTHING else in the
// project needed to change - but move the actual counting logic for
// score, laser ammo, and lives out into three small, plain C# classes
// (ScoreTracker, LaserAmmoTracker, LivesTracker) that know nothing about
// UI, GameObjects, or scenes at all. GameManager now just asks each
// tracker "what happened?" and reacts to the answer by updating the
// screen - it coordinates, rather than personally doing everyone's job.
// Those three classes can now be unit-tested directly and instantly, with
// no scene required (see Assets/Tests/EditMode) - something that was
// simply impossible while this logic was tangled up in here.
//
// Still a static singleton, still one GameManager everything reaches into
// directly - that half of the original tradeoff is unchanged, and still
// perfectly reasonable for a project this size. The part that's genuinely
// better now is that GameManager itself does less, and the parts that
// used to be hardest to verify are the parts that are easiest to test now.
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

    [Header("Lives / Out-of-Bounds Penalty")]
    [Tooltip("How many times the ship can be caught outside the track before it's actually game over. This is completely separate from hitting a real obstacle, which still ends the run instantly regardless of lives remaining.")]
    // FormerlySerializedAs matters here: this field used to just be called
    // "lives" and be read/written directly by everything. Renaming it
    // to startingLives (to make room for a `lives` PROPERTY below that
    // reads/writes the live LivesTracker instead) would otherwise silently
    // reset every scene/prefab that already had a custom value configured
    // here back to this default the next time Unity re-serializes it -
    // this attribute tells Unity "this is the same data as the old
    // 'lives' field, just under a new name," so nothing already set in
    // the Inspector gets lost.
    [FormerlySerializedAs("lives")]
    [SerializeField]
    private int startingLives = 3;

    [Tooltip("The three ship-silhouette icons - gets told to update its display every time lives changes, so the UI never has to be manually kept in sync.")]
    public LivesDisplay livesDisplay;

    [Tooltip("Handles the 'Press Any Key to Continue' message and the big countdown - both the very first one when the game starts, and the one that plays after losing a life.")]
    public RespawnSequence respawnSequence;

    [Tooltip("Force-stopped the instant the run ends (see EndGame() below) - without this, if the game ends while the ship happens to be out of bounds, its countdown/flash/looping alarm would otherwise keep re-triggering forever, since there'd be no respawn sequence left around to ever turn it off.")]
    public TrackBoundsPenalty trackBoundsPenalty;

    [Header("Laser Ammo")]
    [Tooltip("How much laser ammo the run starts with.")]
    [FormerlySerializedAs("laserCount")]
    [SerializeField]
    private int startingLaserCount = 30;

    // The three small trackers doing the actual counting - see the class
    // comment above for the full reasoning. Built fresh in Awake() below,
    // from whatever starting values are set in the Inspector.
    private ScoreTracker scoreTracker;
    private LaserAmmoTracker laserAmmoTracker;
    private LivesTracker livesTracker;

    // Kept as a public property (not a field) with the EXACT SAME NAME the
    // old public field had, specifically so LaserSpawner.cs's existing
    // `GameManager.gmInstance.laserCount` read keeps compiling and working
    // completely unchanged. Read-only on purpose - external code has
    // always only ever READ this value (to check "am I out of ammo"), and
    // routing every actual CHANGE through UpdateLaserCount()/AddLasers()
    // below is what keeps the on-screen text guaranteed to stay in sync
    // with the real number.
    public int laserCount => laserAmmoTracker.Count;

    // Same idea as laserCount above, but this one needs a SETTER too -
    // GameManagerTests.cs (and, in principle, anything else that wants to
    // directly configure a starting condition) assigns to this directly,
    // e.g. `gameManager.lives = 1`. The property just forwards both
    // directions to the tracker, so reading and writing "lives" from
    // outside behaves exactly like it always did, even though a plain
    // int field isn't what's actually storing the number anymore.
    public int lives
    {
        get => livesTracker.Remaining;
        set => livesTracker.SetRemaining(value);
    }

    private void Awake()
    {
        // Explicitly requesting 60 FPS here, mainly for Android's benefit.
        // Without this line, Application.targetFrameRate defaults to -1
        // ("run as fast as the platform naturally allows") - and on a lot
        // of Android devices that quietly settles at something like 30fps
        // instead of the 60 you get for free on PC. Half the framerate
        // isn't just choppier to look at - it means Update() (where all
        // your input reading happens) only runs half as often, so every
        // stick movement genuinely takes longer to be noticed and reacted
        // to. That alone is enough to make controls feel sluggish AND make
        // incoming obstacles feel like they're rushing at you faster than
        // they really are, even though nothing about the actual movement
        // or spawn-timing code changed at all. This runs once, in whichever
        // scene's GameManager wakes up first (the menu's or the gameplay
        // one), and stays in effect for the rest of the app's lifetime.
        Application.targetFrameRate = 60;

        // Build the trackers right away, using whatever starting values
        // are currently set - whether that's the Inspector's serialized
        // value (the normal case) or a plain C# default (the case for a
        // test that does `gameObject.AddComponent<GameManager>()` with
        // nothing configured). Either way, laserCount/lives are safely
        // readable and writable from the very first line after this.
        scoreTracker = new ScoreTracker();
        laserAmmoTracker = new LaserAmmoTracker(startingLaserCount);
        livesTracker = new LivesTracker(startingLives);

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
        //
        // livesDisplay IS worth initializing eagerly here though, rather
        // than waiting for the first life to be lost - otherwise the three
        // ship icons would just sit at whatever default color they happen
        // to have in the Editor until the first penalty, instead of
        // correctly showing all three lives filled in from the very start
        // of the run.
        if (livesDisplay != null)
        {
            livesDisplay.SetLivesRemaining(livesTracker.Remaining);
        }
    }

    /// <summary>
    /// Called by TrackBoundsPenalty once the ship has been outside
    /// TrackBounds for longer than its grace period. Spends one life; if
    /// that was the last one, this is a real game over (same EndGame() the
    /// obstacle-collision path already uses). Otherwise, hands off to
    /// RespawnSequence for the "Press Any Key to Continue" + countdown
    /// flow, and resets the ship back to the middle of the track so the
    /// player doesn't resume control still out of bounds and immediately
    /// lose the next life too.
    /// </summary>
    public void LoseLife()
    {
        bool isGameOver = livesTracker.LoseOne();

        if (livesDisplay == null)
        {
            Debug.LogWarning($"[GameManager] 'livesDisplay' isn't assigned on '{name}' - lives are still being tracked correctly, they just aren't shown on screen.", this);
        }
        else
        {
            livesDisplay.SetLivesRemaining(livesTracker.Remaining);
        }

        if (isGameOver)
        {
            EndGame();
            return;
        }

        if (respawnSequence == null)
        {
            Debug.LogWarning($"[GameManager] 'respawnSequence' isn't assigned on '{name}' - a life was spent, but there's no 'Press Any Key to Continue' flow to hand off to, so the game will just keep running as-is.", this);
            return;
        }

        // Passing the remaining count along here (rather than
        // RespawnSequence just asking GameManager for it later) is what
        // lets RespawnSequence know WHICH icon to flash once gameplay
        // resumes, without needing its own reference back to GameManager -
        // it just remembers whatever number it was handed at the start of
        // this sequence.
        respawnSequence.BeginRespawnSequence(livesTracker.Remaining);
    }

    /// <summary>Ends the current run: hides gameplay UI and shows the final score screen.</summary>
    public void EndGame()
    {
        DisableGameplayUI();
        EnableFinalScoreUI();

        // Covers BOTH ways a run can end while the ship happens to be
        // outside the track: its own grace timer running out (which
        // already turns its feedback off right before calling LoseLife(),
        // so this is mostly a safety net there), and dying to an obstacle
        // collision mid-flash from a not-yet-expired warning (where
        // nothing else would ever turn it off at all). Safe to call even
        // when the ship was never out of bounds in the first place -
        // ForceStopTracking() just does nothing visible in that case.
        if (trackBoundsPenalty != null)
        {
            trackBoundsPenalty.ForceStopTracking();
        }
    }

    /// <summary>Loads the main gameplay scene.</summary>
    public void LoadGame()
    {
        // Loading by NAME instead of by build index (0, 1, 2...) on purpose
        // here. A hardcoded number like SceneManager.LoadScene(0) only
        // means "whatever scene happens to currently sit in that exact slot
        // in File > Build Settings" - so the moment you reorder scenes
        // there (like moving the menu scene to the top so it plays first),
        // this line silently starts pointing at a completely different
        // scene than the one it was written for, with no error or warning
        // anywhere. That's exactly what caused "New Game" to just reload
        // the menu over and over - this used to be scene index 0 back when
        // GamePlayScene WAS index 0, and it never got updated after the
        // build order changed. Loading by name instead means this line
        // keeps working correctly no matter what order your scenes are
        // listed in going forward.
        SceneManager.LoadScene("GamePlayScene");
    }

    public void UpdateScore()
    {
        int newScore = scoreTracker.AddPoints();

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
            scoreText.text = "Score = " + newScore;
        }

        if (finalScoreText == null)
        {
            Debug.LogWarning($"[GameManager] 'finalScoreText' isn't assigned on '{name}' - the game-over screen won't show a number until this is wired up.", this);
        }
        else
        {
            finalScoreText.text = "Final Score = " + newScore;
        }
    }

    /// <summary>Called every time the player fires a laser - spends one shot of ammo.</summary>
    public void UpdateLaserCount()
    {
        // The actual ammo count always spends correctly (or safely does
        // nothing if it was already at 0), whether or not the on-screen
        // text is wired up - firing shouldn't stop working just because a
        // UI label is missing. Only bother touching the text at all if a
        // shot was genuinely spent.
        if (!laserAmmoTracker.TrySpendOne())
        {
            return;
        }

        UpdateLaserCountText("ammo is still being spent correctly, it just isn't shown on screen");
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
        laserAmmoTracker.Add(amount);
        UpdateLaserCountText("the bonus ammo was still granted, it just isn't shown on screen");
    }

    // Shared by UpdateLaserCount() and AddLasers() above - both need to do
    // the exact same "show the current ammo count, or warn once if there's
    // nowhere to show it" work after changing the number by a different
    // amount, so the actual text-updating logic lives here just once.
    private void UpdateLaserCountText(string warningContext)
    {
        if (laserCountText == null)
        {
            Debug.LogWarning($"[GameManager] 'laserCountText' isn't assigned on '{name}' - {warningContext}.", this);
            return;
        }

        laserCountText.text = "Laser = " + laserAmmoTracker.Count;
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
