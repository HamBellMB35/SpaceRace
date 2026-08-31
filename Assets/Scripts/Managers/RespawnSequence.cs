using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

// Drives two closely related moments in the game: the very first "get
// ready" countdown when a run begins, and the "you lost a life, press a
// key when you're ready, then get ready again" flow after an
// out-of-bounds penalty. Both end up running the exact same big countdown
// at the end - the only difference is whether there's a
// "Press Any Key to Continue" message in front of it.
//
// PAUSING: this uses Time.timeScale = 0 for the whole sequence rather than
// just disabling the ship's own scripts. That matters because obstacles,
// enemies, and the scrolling level are all still driven by Time.deltaTime
// elsewhere in the project (SpaceChunksGenerator's WaitForSeconds,
// EnemyForwardMove's transform.Translate, etc.) - if only the ship froze
// while everything else kept moving, you could get hit by something while
// completely unable to react, which would feel awful. Setting timeScale to
// 0 freezes essentially everything in this project uniformly, for free,
// without needing to hunt down and individually pause every other script.
//
// Because WaitForSeconds respects Time.timeScale (and would therefore
// never elapse while paused), this script uses WaitForSecondsRealtime
// instead for its own countdown timing - that's the one exception that
// needs to keep running in real time even while gameplay is frozen.
public class RespawnSequence : MonoBehaviour
{
    [Header("Ship / Track References")]
    [Tooltip("Disabled for the duration of the pause so the ship can't drift, and re-enabled the instant gameplay resumes.")]
    public PlayerMovement playerMovement;

    [Tooltip("Disabled for the duration of the pause so a stray 'press any key' input can't also fire a shot.")]
    public LaserSpawner laserSpawner;

    [Tooltip("Used to reset the ship back to the middle of the real track path when respawning after a lost life, so the player doesn't resume control still out of bounds.")]
    public TrackBounds trackBounds;

    [Tooltip("Paused (isTrackingEnabled = false) for the duration of the sequence, so the same out-of-bounds excursion that just cost a life doesn't immediately start counting down again before the player has regained control.")]
    public TrackBoundsPenalty trackBoundsPenalty;

    [Tooltip("Granted a brief invulnerability window right as gameplay resumes after a respawn - without this, respawning near the same obstacle that just cost a life could immediately cost a second one before the player can react.")]
    public PlayerDeath playerDeath;

    [Tooltip("Told to flash/pulse the just-lost life icon and play a sound, right as gameplay resumes after a respawn - deliberately NOT the instant the life is actually lost, since that moment is hidden behind the 'Press Any Key to Continue' screen and countdown, where the player isn't looking at the icons anyway.")]
    public LivesDisplay livesDisplay;

    [Tooltip("How long the post-respawn invulnerability window lasts, in real seconds, starting from the moment control is handed back to the player.")]
    public float respawnInvulnerabilityDuration = 2f;

    [Tooltip("How long the ship is invincible for right when the game FIRST starts, in real seconds, starting from the moment the opening countdown finishes and control is handed to the player. Separate from respawnInvulnerabilityDuration above so you can tune the very first moment of a run differently from a mid-run respawn - e.g. a little longer, to give the player time to get oriented before anything can hurt them.")]
    public float gameStartInvulnerabilityDuration = 3f;

    [Header("UI")]
    [Tooltip("The 'Press Any Key to Continue' message object - only shown after losing a life (not on the very first game-start countdown), and only until the player actually presses something.")]
    public GameObject pressAnyKeyMessage;

    // CHANGED from UnityEngine.UI.Text to TMP_Text - same reason as
    // TrackBoundsPenalty's countdownText: the actual object in the scene is
    // a TextMeshPro text, and a field typed as the old Text won't accept
    // one of those, even though they both just look like "a text label" in
    // the Hierarchy. TMP_Text's .text property works the same way the old
    // one did, so RunCountdown() below didn't need any changes.
    [Tooltip("The big center-screen countdown text (e.g. '3', '2', '1'). Must be a TextMeshPro text (TMP_Text) - a legacy UI.Text object won't fit in this slot.")]
    public TMP_Text bigCountdownText;

    [Header("Timing")]
    [Tooltip("The countdown counts down from this many whole seconds (e.g. 3 shows '3', then '2', then '1', one second apart in real time).")]
    public int countdownSeconds = 3;

    [Header("Audio")]
    [Tooltip("Plays the beep below. Needs its own AudioSource component - the easiest setup is adding an AudioSource to this same GameObject and dragging it in here. Its own Play On Awake should be left OFF, since this script is the one deciding exactly when each beep plays.")]
    public AudioSource countdownAudioSource;

    [Tooltip("The short beep sound played once for every number in the countdown - once for '3', once for '2', once for '1', and so on.")]
    public AudioClip countdownBeepSound;

    // The Rigidbody actually being reset on respawn - fetched once from
    // playerMovement's own GameObject rather than requiring yet another
    // Inspector field, since it's always going to be the same object.
    private Rigidbody shipRigidbody;

    // The onAnyButtonPress subscription - same IDisposable pattern used in
    // FinalScoreUI.cs, and for the same reason: it's the New Input
    // System's one clean way to say "wake up on literally any input
    // device," covering keyboard, mouse, gamepad, and touch all at once.
    private IDisposable anyButtonPressListener;

    // Set by BeginRespawnSequence() to whichever icon index GameManager
    // says just changed, and read (then cleared back to -1) once the
    // effect actually plays. -1 means "nothing pending" - that's how
    // GameStartRoutine's very first countdown correctly does NOT trigger
    // any flash, since nothing ever sets this away from -1 on that path.
    private int pendingLostLifeIndex = -1;

    private void Awake()
    {
        if (playerMovement != null)
        {
            shipRigidbody = playerMovement.GetComponent<Rigidbody>();
        }

        // Both start hidden - BeginGameStartSequence()/BeginRespawnSequence()
        // are responsible for showing whichever pieces they actually need.
        if (pressAnyKeyMessage != null)
        {
            pressAnyKeyMessage.SetActive(false);
        }

        if (bigCountdownText != null)
        {
            bigCountdownText.gameObject.SetActive(false);
        }
    }

    // Automatically kicks off the very first countdown the moment the
    // gameplay scene loads - this is what makes the countdown also happen
    // "when the game first starts," without GameManager or anything else
    // needing to know to trigger it explicitly.
    private void Start()
    {
        StartCoroutine(GameStartRoutine());
    }

    private IEnumerator GameStartRoutine()
    {
        SetGameplayPaused(true);
        yield return RunCountdown();
        SetGameplayPaused(false);

        // NEW: this was the actual gap - RespawnRoutine() below already
        // grants a temporary invulnerability window right as control comes
        // back after losing a life, but the very FIRST time the game
        // starts never granted one at all. Placed after SetGameplayPaused
        // (not before) for the same reason RespawnRoutine does it in that
        // order too: the window is meant to cover the vulnerable moment
        // right as the player actually starts moving, not to burn itself
        // down while nothing can move yet during the countdown.
        if (playerDeath != null)
        {
            playerDeath.GrantInvulnerability(gameStartInvulnerabilityDuration);
        }
    }

    /// <summary>
    /// Called by GameManager.LoseLife() whenever a life is spent but the
    /// run isn't over. Resets the ship to the middle of the track, shows
    /// "Press Any Key to Continue," waits for input, then runs the same
    /// big countdown as a normal game start before handing control back.
    /// </summary>
    /// <param name="livesRemainingAfterLoss">
    /// The lives count AFTER this loss was already subtracted - GameManager
    /// hands this over so this script knows exactly which of the three life
    /// icons just flipped to "spent" (it's the icon at this same index),
    /// without needing its own separate reference back to GameManager just
    /// to ask.
    /// </param>
    public void BeginRespawnSequence(int livesRemainingAfterLoss)
    {
        pendingLostLifeIndex = livesRemainingAfterLoss;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        SetGameplayPaused(true);
        ResetShipToTrackCenter();

        if (pressAnyKeyMessage != null)
        {
            pressAnyKeyMessage.SetActive(true);
        }

        yield return WaitForAnyButtonPress();

        if (pressAnyKeyMessage != null)
        {
            pressAnyKeyMessage.SetActive(false);
        }

        yield return RunCountdown();
        SetGameplayPaused(false);

        // Granted AFTER control is actually handed back (not while still
        // paused) - the window is meant to cover the vulnerable moment
        // right as the player regains control near wherever they respawned,
        // not to burn itself down uselessly while nothing can move yet.
        if (playerDeath != null)
        {
            playerDeath.GrantInvulnerability(respawnInvulnerabilityDuration);
        }

        // Same "right as control comes back" moment as the invulnerability
        // grant above, and for a similar reason: this is the first instant
        // the player is actually looking at the game again instead of the
        // press-any-key/countdown screen, so it's the right time to draw
        // their eye to "hey, you lost a life" - not back when LoseLife()
        // first ran, while that screen was still covering everything up.
        if (livesDisplay != null && pendingLostLifeIndex >= 0)
        {
            livesDisplay.PlayLifeLostEffect(pendingLostLifeIndex);
            pendingLostLifeIndex = -1;
        }
    }

    // Snaps the ship back to the horizontal/vertical center of the real
    // track path (the midpoint of TrackBounds), zeroes out its velocity,
    // and leaves its Z position untouched - there's no reason to lose
    // forward progress along the track just because a life was spent, only
    // the X/Y drift that got it flagged as out-of-bounds in the first place.
    private void ResetShipToTrackCenter()
    {
        if (playerMovement == null || trackBounds == null)
        {
            return;
        }

        float centerX = (trackBounds.pathMinX + trackBounds.pathMaxX) * 0.5f;
        float centerY = (trackBounds.pathMinY + trackBounds.pathMaxY) * 0.5f;

        Vector3 currentPosition = playerMovement.transform.position;
        playerMovement.transform.position = new Vector3(centerX, centerY, currentPosition.z);

        if (shipRigidbody != null)
        {
            shipRigidbody.velocity = Vector3.zero;
            shipRigidbody.angularVelocity = Vector3.zero;
        }
    }

    // Subscribes to onAnyButtonPress and yields until it fires exactly
    // once, then immediately unsubscribes - same "any device, any button"
    // coverage as FinalScoreUI.cs, just wrapped as something a coroutine
    // can wait on instead of a permanent OnEnable/OnDisable subscription.
    private IEnumerator WaitForAnyButtonPress()
    {
        bool pressed = false;
        anyButtonPressListener = InputSystem.onAnyButtonPress.Call(_ => pressed = true);

        while (!pressed)
        {
            yield return null;
        }

        anyButtonPressListener.Dispose();
        anyButtonPressListener = null;
    }

    // The shared "3, 2, 1" countdown used by both the game-start sequence
    // and the post-respawn sequence. Uses WaitForSecondsRealtime
    // specifically because Time.timeScale is 0 for the whole time this
    // runs (see the class comment up top) - a normal WaitForSeconds would
    // simply never elapse under those conditions.
    private IEnumerator RunCountdown()
    {
        if (bigCountdownText == null)
        {
            yield break;
        }

        bigCountdownText.gameObject.SetActive(true);

        for (int count = countdownSeconds; count >= 1; count--)
        {
            bigCountdownText.text = count.ToString();
            PlayCountdownBeep();
            yield return new WaitForSecondsRealtime(1f);
        }

        bigCountdownText.gameObject.SetActive(false);
    }

    // Plays one beep, right at the same moment the number on screen
    // changes - that's the whole trick to keeping the sound "attached" to
    // the countdown instead of drifting out of sync with it: there's no
    // separate timer for the beep, it just happens as a side effect of the
    // exact same loop iteration that updates the text.
    //
    // Worth knowing for later: AudioSource playback isn't affected by
    // Time.timeScale the way most of this project's Update()/coroutine
    // logic is - the audio engine keeps running in real time on its own,
    // regardless of what timeScale is set to. That's actually convenient
    // here, since this whole sequence deliberately sets timeScale to 0 (see
    // the class comment up top) - the beep plays at a normal, correct pitch
    // and speed without needing any special handling to work around the
    // pause, the same way WaitForSecondsRealtime already sidesteps it for
    // the timing itself.
    private void PlayCountdownBeep()
    {
        if (countdownAudioSource == null || countdownBeepSound == null)
        {
            return;
        }

        // PlayOneShot (rather than assigning .clip and calling Play())
        // means this doesn't fight with whatever clip might already be set
        // directly on the AudioSource, and it's safe to call repeatedly in
        // quick succession without cutting itself off early.
        countdownAudioSource.PlayOneShot(countdownBeepSound);
    }

    // One shared place that both pauses AND resumes everything the
    // sequence needs to touch, so BeginGameStartSequence and
    // BeginRespawnSequence can't accidentally drift out of sync with each
    // other about what "paused" actually means.
    private void SetGameplayPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;

        if (playerMovement != null)
        {
            playerMovement.enabled = !paused;
        }

        if (laserSpawner != null)
        {
            laserSpawner.enabled = !paused;
        }

        if (trackBoundsPenalty != null)
        {
            // Only relevant when PAUSING (a respawn is starting): silence
            // and hide whatever out-of-bounds feedback might currently be
            // active, right away. This covers dying to an obstacle while
            // still mid-flash from a not-yet-expired warning - a death
            // that never goes through TrackBoundsPenalty's own cleanup at
            // all, so without this, the alarm/flash would otherwise just
            // keep going, uninterrupted, all the way through the whole
            // respawn sequence. See ClearActiveFeedback()'s own comment
            // for the full story.
            if (paused)
            {
                trackBoundsPenalty.ClearActiveFeedback();
            }

            trackBoundsPenalty.isTrackingEnabled = !paused;
        }
    }
}