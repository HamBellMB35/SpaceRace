using UnityEngine;

// The single source of truth for "where is the actual track path" - kept
// deliberately separate from PlayerMovement's own min/max fields.
//
// Those PlayerMovement fields used to do double duty: they were both "how
// far can the ship physically move" AND "where does the track visually
// end," which is exactly why they drifted out of sync with the real chunk
// geometry (remember the asymmetric X bounds we found earlier - center at
// X=5 instead of X=0, where the chunks actually spawn). Now that the ship
// is allowed to actually leave the path on purpose - so there's something
// real for the edge glow to warn about, and something real for the
// upcoming life/disqualification system to detect - those two ideas need
// to be tracked by two different things:
//
//   - PlayerMovement's bounds are now a much WIDER safety limit, just
//     there to stop the ship from flying off into literal infinity. The
//     ship can freely cross the real track edges without hitting it.
//   - THIS component defines the tighter, real track boundary - the
//     rectangle you actually want the ship to stay inside for the run to
//     "count." TrackEdgeVignette reads its warning distance from here
//     instead of from PlayerMovement now.
//
// Tune pathMinX/pathMaxX/pathMinY/pathMaxY in the Scene view by eye,
// comparing against your actual chunk geometry (the Delimiters objects,
// or the visible track edges), until the glow lines up with where the
// path really ends.
public class TrackBounds : MonoBehaviour
{
    [Header("Track Path Bounds")]
    [Tooltip("Left edge of the actual track path, in world X.")]
    public float pathMinX = -10f;

    [Tooltip("Right edge of the actual track path, in world X.")]
    public float pathMaxX = 10f;

    [Tooltip("Bottom edge of the actual track path, in world Y.")]
    public float pathMinY = -10f;

    [Tooltip("Top edge of the actual track path, in world Y.")]
    public float pathMaxY = 10f;

    // Same safety-net pattern used on PlayerMovement's own bounds - catches
    // the "min and max got swapped or set equal" mistake immediately in
    // the Editor instead of silently producing broken math later.
    private void OnValidate()
    {
        if (pathMinX >= pathMaxX)
        {
            Debug.LogWarning($"[TrackBounds] pathMinX ({pathMinX}) should be less than pathMaxX ({pathMaxX}) on '{name}' - auto-correcting to a safe default range.", this);
            pathMinX = -10f;
            pathMaxX = 10f;
        }

        if (pathMinY >= pathMaxY)
        {
            Debug.LogWarning($"[TrackBounds] pathMinY ({pathMinY}) should be less than pathMaxY ({pathMaxY}) on '{name}' - auto-correcting to a safe default range.", this);
            pathMinY = -10f;
            pathMaxY = 10f;
        }
    }
}