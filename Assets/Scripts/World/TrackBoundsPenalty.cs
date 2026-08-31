using TMPro;
using UnityEngine;

// Watches how long the ship has been continuously outside TrackBounds
// (the real track path, not PlayerMovement's much wider safety limit -
// see TrackBounds.cs for why those two are now separate things), and
// spends a life once that lasts longer than gracePeriod.
//
// The whole point of the grace period is that TrackEdgeVignette already
// warns you the instant you're CLOSE to a wall - if this script punished
// the moment you actually crossed it, that warning would barely matter,
// since you'd already be paying the price before you had any real chance
// to react. This script only starts caring once you're genuinely OUTSIDE
// the path (not just near the edge), and even then gives you a couple of
// seconds to correct course before it actually costs you anything. A
// quick dip out and back in should feel like "I got away with it," not
// "I got punished for a graze."
//
// This is also where the left-side countdown text comes from: while
// you're outside the path, it counts down from gracePeriod to 0, showing
// exactly how much time is left before a life gets spent. It's hidden
// entirely while you're safely inside the track.
public class TrackBoundsPenalty : MonoBehaviour
{
    [Tooltip("The ship's PlayerMovement - used to read its live position.")]
    public PlayerMovement playerMovement;

    [Tooltip("The real track path bounds - being outside THIS (not PlayerMovement's wider safety limit) is what starts the grace timer.")]
    public TrackBounds trackBounds;

    [Tooltip("How many seconds the ship can stay continuously outside the track before a life is spent.")]
    public float gracePeriod = 2f;

    // CHANGED from UnityEngine.UI.Text to TMP_Text: the OutOfBoundsCountdown
    // object in the scene turned out to be a TextMeshPro text, not a legacy
    // Text - those are two different component types under the hood, so a
    // field typed as the old Text simply refuses a TextMeshPro object when
    // you try to drag it in. TMP_Text is the shared base class both
    // TextMeshPro variants (UI and world-space) inherit from, and it has
    // its own .text property just like the old one did, so nothing else in
    // this script needs to change to support it.
    [Tooltip("The left-side countdown text, shown only while the ship is currently outside the track and counting down toward the penalty. Must be a TextMeshPro text (TMP_Text) - a legacy UI.Text object won't fit in this slot.")]
    public TMP_Text countdownText;

    [Tooltip("The 'Out of Bounds' title/heading that sits alongside the countdown number. Kept perfectly in sync with countdownText - both are shown and hidden together from the same SetOutOfBoundsFeedbackActive() call below, so there's no way for one to show without the other. Left as a plain GameObject (not specifically a Text) so this works whether your title is a Text label, an Image, or a whole little panel.")]
    public GameObject outOfBoundsTitle;

    [Header("Flashing")]
    [Tooltip("A CanvasGroup placed on the PARENT object that contains both countdownText and outOfBoundsTitle. Fading one CanvasGroup's alpha fades everything underneath it together, regardless of whether it's a TextMeshPro text, an Image, or a mix of both - that's much simpler than trying to fade each piece's own color separately, especially since they might not even be the same component type. If this isn't set, the flashing simply won't happen (the text will still show/hide correctly, just without the flash).")]
    public CanvasGroup outOfBoundsCanvasGroup;

    [Tooltip("How many full brighten-dim cycles happen per second while flashing. Higher = faster, more urgent-feeling flicker.")]
    public float flashSpeed = 4f;

    [Tooltip("The dimmest the countdown UI gets at the bottom of each flash cycle (0 = fully invisible, 1 = fully opaque). Kept above 0 by default so it dims rather than fully vanishing, which reads as 'flashing' instead of 'flickering broken.'")]
    [Range(0f, 1f)]
    public float minFlashAlpha = 0.25f;

    [Header("Audio")]
    [Tooltip("Plays on a loop for as long as the ship stays outside the track, and stops the instant it comes back in bounds (or the grace period runs out and a life is spent). On the AudioSource component itself: turn Loop ON and Play On Awake OFF - this script is the one deciding exactly when it starts and stops.")]
    public AudioSource outOfBoundsAudioSource;

    // How long, in a row, the ship has currently been outside the track.
    // Resets to 0 the instant the ship comes back inside - this is
    // deliberately NOT a total/lifetime counter, just "how long have you
    // been out right now."
    private float timeOutsideBounds;

    // RespawnSequence flips this off for the duration of the "Press Any
    // Key to Continue" + countdown flow, and back on once gameplay
    // actually resumes. Without this, the exact same out-of-bounds
    // condition that just cost a life would immediately start counting
    // down again the instant the sequence finishes, often before the
    // player has even had a chance to react to being back in control.
    [HideInInspector]
    public bool isTrackingEnabled = true;

    /// <summary>
    /// Called by GameManager.EndGame() the instant the run is completely
    /// over (out of lives). Immediately hides the countdown/title, stops
    /// the looping warning sound, and disables tracking entirely -
    /// regardless of whatever state the flash/sound happened to be in at
    /// that exact moment.
    ///
    /// This exists because of a gap the normal Update() logic doesn't
    /// cover: it only ever turns itself off when the ship comes BACK in
    /// bounds, or when its own grace timer runs out. If the run ends for
    /// ANY other reason while the ship happens to be outside the
    /// track - most notably, dying to an obstacle collision while
    /// mid-flash from a not-yet-expired warning - nothing would otherwise
    /// ever tell this script to stop, and Update() would just keep running
    /// forever with no respawn sequence left around to hand it a clean
    /// "you're done now."
    /// </summary>
    public void ForceStopTracking()
    {
        isTrackingEnabled = false;
        ClearActiveFeedback();
    }

    /// <summary>
    /// Immediately hides the countdown/title, stops the looping warning
    /// sound, and resets the grace timer - like ForceStopTracking() above,
    /// but WITHOUT permanently disabling tracking. Use this for a
    /// TEMPORARY interruption (a respawn is about to begin, but tracking
    /// should resume once it's over) rather than a permanent one (the run
    /// being completely over, where ForceStopTracking() is the right call
    /// instead).
    ///
    /// Called by RespawnSequence the instant ANY respawn begins - not just
    /// the ones caused by this script's own grace timer running out (that
    /// path already stops itself before ever asking for a respawn), but
    /// also the case that was previously falling through the cracks:
    /// dying to an obstacle collision while mid-flash from a
    /// not-yet-expired out-of-bounds warning. That death path never went
    /// through this script's own Update() logic at all, and RespawnSequence
    /// disabling isTrackingEnabled only stops FUTURE updates - it doesn't
    /// silence whatever's already actively playing. Without this method,
    /// the alarm would just keep looping, uninterrupted, all the way
    /// through the respawn sequence, only finally stopping once gameplay
    /// resumed and the ship happened to already be back inside the track.
    /// </summary>
    public void ClearActiveFeedback()
    {
        timeOutsideBounds = 0f;
        SetOutOfBoundsFeedbackActive(false);
    }

    private void Update()
    {
        if (!isTrackingEnabled || playerMovement == null || trackBounds == null)
        {
            return;
        }

        Vector3 shipPosition = playerMovement.transform.position;

        bool isOutsideBounds =
            shipPosition.x < trackBounds.pathMinX || shipPosition.x > trackBounds.pathMaxX ||
            shipPosition.y < trackBounds.pathMinY || shipPosition.y > trackBounds.pathMaxY;

        if (!isOutsideBounds)
        {
            // Back inside the path - clear the timer completely and hide
            // the countdown. Even being 99% of the way through the grace
            // period doesn't carry over to the next time you leave the
            // path; getting back in bounds, even briefly, fully resets it.
            timeOutsideBounds = 0f;
            SetOutOfBoundsFeedbackActive(false);
            return;
        }

        timeOutsideBounds += Time.deltaTime;

        if (timeOutsideBounds >= gracePeriod)
        {
            // Grace period's up - reset immediately (BEFORE calling
            // LoseLife) so that if the respawn sequence somehow doesn't
            // move the ship back in bounds instantly, this script doesn't
            // fire again next frame and spend a second life for the same
            // excursion.
            timeOutsideBounds = 0f;
            SetOutOfBoundsFeedbackActive(false);

            if (GameManager.gmInstance != null)
            {
                GameManager.gmInstance.LoseLife();
            }

            return;
        }

        // Still within the grace period - update the countdown to show
        // exactly how much time is left before it isn't.
        float timeRemaining = gracePeriod - timeOutsideBounds;
        SetOutOfBoundsFeedbackActive(true);
        if (countdownText != null)
        {
            countdownText.text = timeRemaining.ToString("0.0");
        }

        // Flashing needs to update every single frame the whole time we're
        // outside the track (not just once when it first appears), since
        // it's a continuous pulsing effect rather than a one-time state
        // change. That's why this call lives out here in Update() instead
        // of inside SetOutOfBoundsFeedbackActive() below, which only runs
        // at the moment things switch on or off.
        UpdateFlash();
    }

    // Turns the countdown text, its title, and the looping warning sound on
    // or off together, all from one place - this is the single spot that
    // decides what "actively out of bounds" looks and sounds like, so the
    // three pieces can never end up out of sync with each other (e.g. the
    // sound still looping after the text has already hidden).
    private void SetOutOfBoundsFeedbackActive(bool active)
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(active);
        }

        if (outOfBoundsTitle != null)
        {
            outOfBoundsTitle.SetActive(active);
        }

        if (!active && outOfBoundsCanvasGroup != null)
        {
            // Reset all the way back to fully opaque whenever we hide the
            // countdown - without this, the NEXT time the ship goes out of
            // bounds, the flash could resume from wherever it happened to
            // leave off (say, mid-dim), producing a jarring one-frame flash
            // instead of a clean, consistent fade-in each time.
            outOfBoundsCanvasGroup.alpha = 1f;
        }

        SetOutOfBoundsAudioPlaying(active);
    }

    // A smooth, continuous brighten-dim pulse using a sine wave rather than
    // an abrupt on/off blink - Mathf.Sin naturally oscillates between -1
    // and 1 over time, so remapping that into the minFlashAlpha..1 range
    // gives a gentle "breathing" flash instead of a harsh strobe. Time.time
    // (not Time.deltaTime accumulation) is used as the input specifically
    // so the flash always has a smooth, consistent rhythm regardless of
    // frame rate - it doesn't drift or stutter if the game briefly lags.
    private void UpdateFlash()
    {
        if (outOfBoundsCanvasGroup == null)
        {
            return;
        }

        float sineWave01 = (Mathf.Sin(Time.time * flashSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        outOfBoundsCanvasGroup.alpha = Mathf.Lerp(minFlashAlpha, 1f, sineWave01);
    }

    // Starts or stops the looping warning sound. The isPlaying check on the
    // "start" side matters a lot here: Update() calls
    // SetOutOfBoundsFeedbackActive(true) on EVERY frame the ship is outside
    // the track (not just the first one), so without that check this would
    // call .Play() dozens of times a second, which would keep restarting
    // the clip from the beginning over and over instead of letting it
    // actually loop.
    private void SetOutOfBoundsAudioPlaying(bool playing)
    {
        if (outOfBoundsAudioSource == null)
        {
            return;
        }

        if (playing)
        {
            if (!outOfBoundsAudioSource.isPlaying)
            {
                outOfBoundsAudioSource.Play();
            }
        }
        else
        {
            outOfBoundsAudioSource.Stop();
        }
    }
}