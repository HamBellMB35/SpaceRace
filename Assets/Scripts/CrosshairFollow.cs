using UnityEngine;

// Keeps the crosshair UI element positioned over LaserSpawner's current
// aim point, projected from world space onto the screen. Attach this to
// the Crosshair Image object itself (the one under your gameplay Canvas).
//
// This is what makes the reticle actually move as the ship slides around
// in frame, instead of sitting frozen at the center of the screen - it's
// reading the exact same CurrentAimPoint value LaserSpawner is about to
// fire toward, so the visual reticle and the actual shot direction can
// never disagree with each other.
[RequireComponent(typeof(RectTransform))]
public class CrosshairFollow : MonoBehaviour
{
    [Tooltip("The LaserSpawner to read the live aim point from - this should be the same one attached to your ship.")]
    public LaserSpawner laserSpawner;

    [Tooltip("The camera to project the world-space aim point through. Leave empty to just use Camera.main.")]
    public Camera targetCamera;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    // LateUpdate rather than Update: we want this to run after everything
    // else has finished moving for the frame - the ship, and especially
    // the Cinemachine camera (which itself updates in LateUpdate) - so
    // we're projecting through the camera's final position for this frame,
    // not a stale one from a moment ago. Using plain Update here could
    // introduce a subtle one-frame lag depending on script execution
    // order, which LateUpdate sidesteps.
    private void LateUpdate()
    {
        if (laserSpawner == null || targetCamera == null)
        {
            return;
        }

        Vector3 screenPoint = targetCamera.WorldToScreenPoint(laserSpawner.CurrentAimPoint);

        // WorldToScreenPoint's Z is the distance in front of the camera -
        // if it's negative, the aim point is actually behind the camera,
        // which would otherwise project to a nonsense screen position. In
        // normal play this shouldn't happen (the aim point is always out
        // ahead of the ship, which the camera is looking toward), but it's
        // a cheap safety check against a jarring crosshair glitch if it
        // ever does.
        if (screenPoint.z > 0f)
        {
            rectTransform.position = screenPoint;
        }
    }
}