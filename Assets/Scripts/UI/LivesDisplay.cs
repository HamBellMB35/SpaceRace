using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Shows lives remaining as three ship-silhouette icons, center-top of the
// screen. Each icon is actually built from TWO stacked Image components
// sharing the same shape - a solid Fill underneath and a hollow Outline on
// top - because that's what lets a single icon represent two completely
// different-looking states without needing two entirely separate sprites
// per state:
//
//   - Life still available: Fill is tinted your chosen color, Outline is
//     also that color sitting right on top of it - the combined look is a
//     solid, fully colored ship.
//   - Life already lost: Fill switches to a dark gray/black, while Outline
//     STAYS the same chosen color - so what you see is a colored outline
//     with a dark, "hollowed out" interior, exactly like a used-up icon in
//     a lot of arcade-style lives displays.
//
// GameManager calls SetLivesRemaining() every time lives changes (both to
// initialize the display at the start of a run, and after each life is
// lost), so this script itself never needs to know WHY the count changed -
// it just reflects whatever number it's given.
//
// PlayLifeLostEffect() is a separate, second thing this script can do: a
// brief flash-and-pulse-and-beep on ONE specific icon, called by
// RespawnSequence right as gameplay resumes after a respawn - not by
// SetLivesRemaining() itself, and not at the moment the life is actually
// lost. That timing gap is deliberate: the instant a life is lost, the
// "Press Any Key to Continue" screen and countdown are about to cover the
// whole thing up anyway, so a flash right then would be wasted on a player
// who isn't even looking at the icons yet. See RespawnSequence.cs for where
// that call actually happens.
public class LivesDisplay : MonoBehaviour
{
    [Tooltip("The color a life is shown as while it's still available - both the fill and the outline use this color when a life is intact.")]
    public Color livesColor = new Color(0.2f, 0.85f, 1f, 1f);

    [Tooltip("The color the FILL switches to once that life has been lost. The outline keeps using livesColor regardless, so you still see a colored outline, just with a dark/empty interior instead of a solid fill.")]
    public Color lostLifeFillColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    [Tooltip("The three solid 'Fill' Images, one per life icon, index 0 to 2. Each should be the ShipLifeIcon_Fill sprite.")]
    public Image[] fillIcons = new Image[3];

    [Tooltip("The three hollow 'Outline' Images, one per life icon, index 0 to 2, each stacked directly on top of the matching fillIcons entry. Each should be the ShipLifeIcon_Outline sprite.")]
    public Image[] outlineIcons = new Image[3];

    [Header("Life Lost Effect")]
    [Tooltip("Plays once every time PlayLifeLostEffect() runs. Needs its own AudioSource component - Play On Awake should be OFF, since this script decides exactly when it fires.")]
    public AudioSource lifeLostAudioSource;

    [Tooltip("The sound played alongside the flash/pulse - something short and attention-grabbing works best, since this is meant to be a quick 'heads up' cue, not a lingering one.")]
    public AudioClip lifeLostSound;

    [Tooltip("How long the flash-and-pulse effect lasts, in real seconds, before the icon settles back to its normal resting look.")]
    public float lifeLostEffectDuration = 1f;

    [Tooltip("How many full brighten-dim-and-grow-shrink pulses happen per second during the effect. Higher = faster, more urgent-feeling flicker.")]
    public float lifeLostFlashSpeed = 6f;

    [Tooltip("How much bigger than normal the icon grows at the peak of each pulse. 1 = no growth at all, 1.3 = 30% bigger at the biggest point of each pulse.")]
    public float lifeLostPulseScale = 1.3f;

    // Runs both at Start AND whenever a field changes in the Editor (via
    // OnValidate below) - this is what lets you preview how the chosen
    // colors will actually look without needing to enter Play mode and
    // manually lose a life just to see it.
    private void Start()
    {
        RefreshOutlineColors();
    }

    private void OnValidate()
    {
        RefreshOutlineColors();
    }

    // The outline color never depends on how many lives are left - it's
    // always livesColor, on every icon, all the time. Splitting this out
    // from SetLivesRemaining() means changing livesColor in the Inspector
    // (say, mid-Play-mode while tuning it) updates all three outlines
    // immediately, without needing lives to actually change first.
    private void RefreshOutlineColors()
    {
        for (int i = 0; i < outlineIcons.Length; i++)
        {
            if (outlineIcons[i] != null)
            {
                outlineIcons[i].color = livesColor;
            }
        }
    }

    /// <summary>
    /// Updates all three icons to reflect how many lives are currently
    /// remaining. Icons with an index LESS than livesRemaining show as
    /// intact (filled with livesColor); everything else shows as spent
    /// (filled with lostLifeFillColor). Called by GameManager both to
    /// initialize the display at the start of a run and after every
    /// LoseLife() call.
    /// </summary>
    public void SetLivesRemaining(int livesRemaining)
    {
        RefreshOutlineColors();

        for (int i = 0; i < fillIcons.Length; i++)
        {
            if (fillIcons[i] == null)
            {
                Debug.LogWarning($"[LivesDisplay] fillIcons[{i}] isn't assigned on '{name}' - that life icon won't update.", this);
                continue;
            }

            bool lifeIntact = i < livesRemaining;
            fillIcons[i].color = lifeIntact ? livesColor : lostLifeFillColor;
        }
    }

    /// <summary>
    /// Flashes and pulses ONE specific icon (the one that just changed to
    /// "spent") and plays lifeLostSound alongside it. Called by
    /// RespawnSequence right as gameplay resumes after a respawn - see the
    /// class comment above for why the timing is deliberately delayed
    /// rather than instant.
    /// </summary>
    /// <param name="iconIndex">
    /// Index into fillIcons/outlineIcons of the icon to flash - this is
    /// always the SAME number as livesRemaining right after a life is lost
    /// (e.g. losing the 3rd life brings livesRemaining to 2, and index 2 is
    /// exactly the icon that just flipped from intact to spent).
    /// </param>
    public void PlayLifeLostEffect(int iconIndex)
    {
        if (iconIndex < 0 || iconIndex >= fillIcons.Length)
        {
            Debug.LogWarning($"[LivesDisplay] PlayLifeLostEffect got an out-of-range icon index ({iconIndex}) on '{name}' - there's no matching icon to flash.", this);
            return;
        }

        if (lifeLostAudioSource != null && lifeLostSound != null)
        {
            lifeLostAudioSource.PlayOneShot(lifeLostSound);
        }

        StartCoroutine(FlashAndPulseIcon(iconIndex));
    }

    // Grows/shrinks and brightens/dims the one icon at iconIndex over
    // lifeLostEffectDuration, then snaps everything back to exactly how it
    // looked before the effect started. Scaling and coloring BOTH the fill
    // and outline pieces together (rather than just one) is what makes the
    // whole icon look like it's pulsing as a single unit, since they're
    // stacked directly on top of each other.
    private IEnumerator FlashAndPulseIcon(int iconIndex)
    {
        Image fillImage = fillIcons[iconIndex];
        Image outlineImage = iconIndex < outlineIcons.Length ? outlineIcons[iconIndex] : null;

        // Remember exactly how this icon looked right before the effect
        // started - its actual current color (whichever it was, intact or
        // already-spent) and its normal scale - so the effect can restore
        // it EXACTLY afterward, rather than assuming some fixed "default"
        // that might not match what SetLivesRemaining() actually set.
        Color fillRestColor = fillImage != null ? fillImage.color : Color.white;
        Color outlineRestColor = outlineImage != null ? outlineImage.color : Color.white;
        Vector3 fillRestScale = fillImage != null ? fillImage.transform.localScale : Vector3.one;
        Vector3 outlineRestScale = outlineImage != null ? outlineImage.transform.localScale : Vector3.one;

        float elapsed = 0f;
        while (elapsed < lifeLostEffectDuration)
        {
            // Time.unscaledDeltaTime rather than Time.deltaTime: by the
            // time this runs, RespawnSequence has already set
            // Time.timeScale back to 1, so in practice these behave the
            // same right now - but using unscaled time here means this
            // effect would still animate correctly even if it were ever
            // triggered during a moment gameplay was paused for some other
            // reason in the future, the same defensive habit this project
            // already uses for every other UI timer.
            elapsed += Time.unscaledDeltaTime;

            // A smooth 0..1..0..1 pulse via a sine wave - the same trick
            // TrackBoundsPenalty.cs uses for its out-of-bounds flash -
            // rather than an abrupt on/off blink.
            float pulse01 = (Mathf.Sin(elapsed * lifeLostFlashSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            float scaleMultiplier = Mathf.Lerp(1f, lifeLostPulseScale, pulse01);

            if (fillImage != null)
            {
                fillImage.transform.localScale = fillRestScale * scaleMultiplier;

                // Flash brightness by pulsing alpha between dim and fully
                // opaque, layered on top of whatever color it already was
                // (livesColor or lostLifeFillColor) rather than replacing
                // that color outright.
                Color flashed = fillRestColor;
                flashed.a = Mathf.Lerp(fillRestColor.a * 0.4f, fillRestColor.a, pulse01);
                fillImage.color = flashed;
            }

            if (outlineImage != null)
            {
                outlineImage.transform.localScale = outlineRestScale * scaleMultiplier;

                Color flashedOutline = outlineRestColor;
                flashedOutline.a = Mathf.Lerp(outlineRestColor.a * 0.4f, outlineRestColor.a, pulse01);
                outlineImage.color = flashedOutline;
            }

            yield return null;
        }

        // Snap back to the EXACT remembered rest state, rather than trusting
        // the last loop iteration to have landed precisely there - the loop
        // can exit slightly past a "clean" pulse value depending on frame
        // timing, which would otherwise leave the icon a hair off from
        // where it's supposed to settle.
        if (fillImage != null)
        {
            fillImage.transform.localScale = fillRestScale;
            fillImage.color = fillRestColor;
        }

        if (outlineImage != null)
        {
            outlineImage.transform.localScale = outlineRestScale;
            outlineImage.color = outlineRestColor;
        }
    }
}