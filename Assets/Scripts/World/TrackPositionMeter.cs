using UnityEngine;

// The "precise numbers" half of the centering solution: two thin
// instrument-style bars (one for left/right offset, one for up/down
// offset), each with a marker that slides along the bar to show exactly
// where the ship sits between the track's min and max bounds on that
// axis, plus a fixed center tick (just a plain UI Image you place by hand
// in the Editor - it doesn't move, so it doesn't need a script) marking
// dead center for reference.
//
// This is deliberately built around two RectTransforms per axis - a
// "track" (the background bar, whose SIZE defines how far the marker is
// allowed to slide) and a "marker" (the little indicator that actually
// moves) - rather than hard-coding pixel distances anywhere. That means
// you can resize either bar in the Editor at any time and the marker's
// travel range automatically matches, instead of the marker overshooting
// or undershooting the bar because some pixel number got out of sync with
// how big you drew it.
public class TrackPositionMeter : MonoBehaviour
{
    [Tooltip("The ship's PlayerMovement component - both live position and track bounds are read from here.")]
    public PlayerMovement playerMovement;

    [Header("Horizontal (X) Meter")]
    [Tooltip("The background bar for left/right position. Its WIDTH defines how far the marker can slide left or right - make sure its pivot/anchor is centered.")]
    public RectTransform horizontalTrack;

    [Tooltip("The marker that slides left/right along horizontalTrack to show X position.")]
    public RectTransform horizontalMarker;

    [Header("Vertical (Y) Meter")]
    [Tooltip("The background bar for up/down position. Its HEIGHT defines how far the marker can slide up or down - make sure its pivot/anchor is centered.")]
    public RectTransform verticalTrack;

    [Tooltip("The marker that slides up/down along verticalTrack to show Y position.")]
    public RectTransform verticalMarker;

    private void LateUpdate()
    {
        if (playerMovement == null)
        {
            return;
        }

        Vector3 shipPosition = playerMovement.transform.position;

        float normalizedX = Mathf.InverseLerp(playerMovement.minMovementX, playerMovement.maxMovementX, shipPosition.x);
        float normalizedY = Mathf.InverseLerp(playerMovement.minMovementY, playerMovement.maxMovementY, shipPosition.y);

        PositionMarker(horizontalTrack, horizontalMarker, normalizedX, isHorizontal: true);
        PositionMarker(verticalTrack, verticalMarker, normalizedY, isHorizontal: false);
    }

    // Slides one marker along one track, given a normalized (0-1) position
    // on that axis. Shared between both the horizontal and vertical meters
    // via the isHorizontal flag, rather than writing the same "map 0-1 onto
    // a range and set anchoredPosition" logic out twice.
    private void PositionMarker(RectTransform track, RectTransform marker, float normalized, bool isHorizontal)
    {
        if (track == null || marker == null)
        {
            return;
        }

        // track.rect.width/height is the track's actual current size in
        // its own local units - reading it live like this (rather than a
        // number typed into the Inspector) is what lets you freely resize
        // the bar later without the marker's range falling out of sync.
        float halfExtent = isHorizontal ? track.rect.width * 0.5f : track.rect.height * 0.5f;

        // Map normalized (0 at the min bound, 1 at the max bound) onto
        // -halfExtent..+halfExtent, so a normalized value of exactly 0.5
        // (dead center) always lands precisely on 0 - right on top of
        // wherever you've placed the fixed center tick mark.
        float offset = Mathf.Lerp(-halfExtent, halfExtent, normalized);

        Vector2 anchoredPosition = marker.anchoredPosition;
        if (isHorizontal)
        {
            anchoredPosition.x = offset;
        }
        else
        {
            anchoredPosition.y = offset;
        }
        marker.anchoredPosition = anchoredPosition;
    }
}