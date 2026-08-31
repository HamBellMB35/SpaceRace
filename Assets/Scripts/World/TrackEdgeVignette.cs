using UnityEngine;
using UnityEngine.UI;

// Solves the "how centered am I in the track?" problem with the lightest
// possible touch: four soft glows along the screen edges, one per wall,
// that fade in as the ship gets close to that specific wall and vanish
// completely when the ship is centered. Glance at the screen edges, not a
// dedicated instrument - which is exactly why this one's paired with
// TrackPositionMeter.cs (precise numbers) and TrackBoundaryRails.cs (the
// wall itself, visible in 3D) rather than trying to be all three at once.
//
// TRACK BOUNDS, changed: this used to read its min/max straight from
// PlayerMovement's own bounds fields. Those fields now do a DIFFERENT job
// - they're a wide safety limit stopping the ship from flying off into
// infinity, not the real track edge - since the ship is now allowed to
// actually leave the path on purpose. The real track edge lives on the
// new TrackBounds component instead, which is what this script reads its
// min/max from now. playerMovement is still needed too, just for a
// different reason: reading the ship's live position via its Transform.
//
// GLOW START FRACTION, now per-edge: this used to be one shared
// glowStartFraction value driving all four edges identically. That's a
// reasonable starting point, but in practice different walls often
// deserve different warning distances - for example, if the track is much
// wider than it is tall, you might want the left/right glow to start
// earlier (since there's more room to react) while top/bottom starts
// later (since there's less vertical room and you're closer to it more
// often anyway). Splitting this into four independent fields means each
// edge's warning distance can be tuned separately without the others
// having to compromise on a single shared number.
public class TrackEdgeVignette : MonoBehaviour
{
    [Tooltip("The ship's PlayerMovement component - used ONLY to read the ship's live position now (transform.position), not its bounds.")]
    public PlayerMovement playerMovement;

    [Tooltip("The TrackBounds component defining where the real track path actually is - THIS is what the glow warns you about now, not PlayerMovement's own (much wider) safety limit.")]
    public TrackBounds trackBounds;

    [Header("Edge Glow Images")]
    [Tooltip("A UI Image stretched along the LEFT edge of the screen, using the gradient sprite. Glows when the ship is near minMovementX.")]
    public Image leftEdge;

    [Tooltip("A UI Image stretched along the RIGHT edge of the screen. Glows when the ship is near maxMovementX.")]
    public Image rightEdge;

    [Tooltip("A UI Image stretched along the TOP edge of the screen. Glows when the ship is near maxMovementY.")]
    public Image topEdge;

    [Tooltip("A UI Image stretched along the BOTTOM edge of the screen. Glows when the ship is near minMovementY.")]
    public Image bottomEdge;

    [Header("Glow Start Distance (per edge)")]
    [Tooltip("How close to the LEFT wall (as a 0-1 fraction of the full track width) before its glow starts appearing at all. 0.6 means nothing shows on this edge until you're 60% of the way from center to the left wall.")]
    [Range(0.05f, 0.95f)]
    public float leftGlowStartFraction = 0.6f;

    [Tooltip("Same idea as leftGlowStartFraction, but for the RIGHT wall - tune independently since the two sides don't have to behave identically.")]
    [Range(0.05f, 0.95f)]
    public float rightGlowStartFraction = 0.6f;

    [Tooltip("Same idea, but for the TOP wall.")]
    [Range(0.05f, 0.95f)]
    public float topGlowStartFraction = 0.6f;

    [Tooltip("Same idea, but for the BOTTOM wall.")]
    [Range(0.05f, 0.95f)]
    public float bottomGlowStartFraction = 0.6f;

    [Header("Feel")]
    [Tooltip("How strong the glow gets once the ship is touching (or past) that wall. Shared across all four edges - if you want different max intensities per edge too, let me know and I'll split this one out the same way.")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.6f;

    private void LateUpdate()
    {
        if (playerMovement == null || trackBounds == null)
        {
            return;
        }

        Vector3 shipPosition = playerMovement.transform.position;

        // Mathf.InverseLerp does exactly the math we need here: "given
        // these min/max bounds, where does this value sit between them, as
        // a 0-1 fraction?" 0 means sitting exactly at the min bound, 1
        // means exactly at the max bound, 0.5 means dead center. The bounds
        // now come from TrackBounds (the real track path) instead of
        // PlayerMovement (which is just a wide safety limit these days) -
        // that's the one line that actually changed here.
        float normalizedX = Mathf.InverseLerp(trackBounds.pathMinX, trackBounds.pathMaxX, shipPosition.x);
        float normalizedY = Mathf.InverseLerp(trackBounds.pathMinY, trackBounds.pathMaxY, shipPosition.y);

        // Once the ship actually crosses outside the path (normalized goes
        // below 0 or above 1), Clamp01 further down would otherwise cap the
        // glow amount at "fully at the wall" and never show anything MORE
        // urgent than that. That's fine for this version (a flat maximum
        // glow while off-path is a perfectly reasonable visual), but it's
        // worth knowing this is where you'd hook in something stronger -
        // like a flashing warning or the future life/disqualification
        // countdown - once the ship is confirmed outside pathMinX..pathMaxX
        // or pathMinY..pathMaxY rather than just close to the edge of them.

        // Split that single 0-1 value per axis into "how close to each of
        // the two walls on that axis," so the left wall and right wall (for
        // example) can glow completely independently of each other rather
        // than fighting over one shared number.
        float leftAmount = Mathf.Clamp01(1f - normalizedX);
        float rightAmount = Mathf.Clamp01(normalizedX);
        float bottomAmount = Mathf.Clamp01(1f - normalizedY);
        float topAmount = Mathf.Clamp01(normalizedY);

        // Each call now passes its OWN start-fraction field, instead of
        // every edge reading the same shared one - this is the whole
        // change.
        SetEdgeAlpha(leftEdge, leftAmount, leftGlowStartFraction);
        SetEdgeAlpha(rightEdge, rightAmount, rightGlowStartFraction);
        SetEdgeAlpha(topEdge, topAmount, topGlowStartFraction);
        SetEdgeAlpha(bottomEdge, bottomAmount, bottomGlowStartFraction);
    }

    // Takes a raw 0-1 "how close to this wall" value and turns it into an
    // actual alpha for the given edge Image - remapped so the glow only
    // starts once you're past that edge's own glowStartFraction, instead
    // of being faintly, distractingly visible all the time. Now takes the
    // start fraction as a parameter rather than reading one shared field,
    // so each of the four calls above can pass in its own value.
    private void SetEdgeAlpha(Image edge, float amount, float glowStartFraction)
    {
        if (edge == null)
        {
            return;
        }

        float remapped = Mathf.InverseLerp(glowStartFraction, 1f, amount);
        Color color = edge.color;
        color.a = remapped * maxAlpha;
        edge.color = color;
    }
}