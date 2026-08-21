using System.Collections;
using UnityEngine;

// Fires laser projectiles at whatever the crosshair is currently pointing
// at, and handles the ammo check before doing so. Ammo itself lives
// entirely in GameManager.laserCount (see GameManager.cs's AddLasers
// method for why we consolidated to a single counter instead of this
// script keeping its own).
//
// TWIN CANNONS, changed again: you had two GameObjects each carrying their
// own LaserSpawner - one script per "gun." That's why a single click was
// firing twice and burning two ammo instead of one - each LaserSpawner had
// its own PlayerControls instance, both listening for the same Fire input,
// both independently deciding "yep, that's a click, spend a shot and fire."
// Neither script knew the other one existed, so there was no way to make
// them agree on "this was one shot."
//
// The fix is to stop treating "one gun" as "one script." Now there's a
// single LaserSpawner (delete the second one from the other object) that
// owns a whole LIST of spawn points instead of just one. One click is
// still exactly one click - one ammo spent, one sound played - but that
// single shot now fires a projectile out of every spawn point in the list
// at once, which is what gives you the visual "twin beams" look without
// double-charging the player for it.
//
// AIMING: the previous version cast a ray from the exact center of the
// screen, which meant the crosshair was effectively glued to the middle of
// the view no matter what the ship was doing. Since the Cinemachine camera
// lags behind the ship on purpose (that's what gives it the chase-cam
// feel), the ship visibly drifts around within the frame - and a
// screen-locked crosshair doesn't track that drift at all, so it stops
// looking connected to the ship.
//
// Now the aim point is computed from the ship itself: a point out ahead of
// it in the direction of travel (aimAnchor.position + forward *
// aimLeadDistance), recalculated every single frame rather than only when
// firing. That point is exposed publicly as CurrentAimPoint specifically
// so CrosshairFollow.cs (on the crosshair UI element) can project the
// exact same point onto the screen - meaning the reticle and the actual
// firing direction are guaranteed to always agree, since they're reading
// from one shared value instead of two separate calculations that could
// drift apart from each other.
//
// We still raycast (from the camera toward that computed point, not from
// the ship) so that if something solid is actually in the way, the shot
// aims precisely at it rather than just flying toward open space past it.
public class LaserSpawner : MonoBehaviour
{
    public GameObject spherePrefab;

    [Tooltip("How much FASTER than the ship itself each shot travels, in units/sec - this gets added ON TOP OF the ship's own current velocity, not used as a fixed absolute speed. That's what guarantees a shot is always outrunning the ship that fired it, no matter how fast the ship happens to be going at that moment.")]
    public float shootForce = 10f;

    [Tooltip("Every muzzle this ship fires from. One click fires one projectile out of EACH entry here - drag in both barrel Transforms for a twin-cannon look, or just one for a single-gun ship. Costs one shot of ammo total, no matter how many entries are here.")]
    public Transform[] spawnPoints;

    public AudioClip laserSound;
    public AudioClip noAmmoSound;

    [Header("Aiming")]
    [Tooltip("The ship's Transform - the aim point is calculated out ahead of this. Must be assigned or aiming falls back to this object's own position.")]
    public Transform aimAnchor;

    [Tooltip("How far ahead of the ship (along world forward) the aim point sits when nothing is in the way.")]
    public float aimLeadDistance = 30f;

    [Tooltip("How far out to aim if the raycast doesn't hit anything - keeps shots fired into empty space still flying in the right direction.")]
    public float maxAimDistance = 500f;

    [Tooltip("Which layers the aim raycast can hit. Defaults to everything.")]
    public LayerMask aimLayerMask = ~0; // ~0 means "all layers" - bitwise NOT of 0 sets every bit to 1

    // The live aim point, recalculated every frame in Update() below.
    // Public (read-only from outside via the property setter being
    // private) specifically so other scripts - right now just
    // CrosshairFollow.cs - can read the exact same point this script is
    // about to fire toward, instead of each script computing its own
    // slightly-different version of "where the crosshair is."
    public Vector3 CurrentAimPoint { get; private set; }

    private PlayerControls controls;

    // The ship's own Rigidbody, cached once here so FireFromPoint() below
    // can read its CURRENT velocity every time a shot is fired - that's
    // what lets a laser inherit the ship's speed and add its own muzzle
    // velocity on top, instead of always launching at some fixed absolute
    // speed regardless of how fast the ship happens to already be moving.
    private Rigidbody shipRigidbody;

    private void Awake()
    {
        controls = new PlayerControls();

        if (aimAnchor == null)
        {
            Debug.LogWarning($"[LaserSpawner] aimAnchor isn't assigned on '{name}' - falling back to this object's own transform, which is probably not what you want. Assign the ship's Transform in the Inspector.", this);
            aimAnchor = transform;
        }

        // GetComponent here (not GetComponentInParent/InChildren) because
        // aimAnchor is expected to be the ship's own root Transform - the
        // same one PlayerMovement's Rigidbody lives on. If this comes back
        // null, shots will simply launch without inheriting any of the
        // ship's velocity (falling back to the old fixed-speed behavior)
        // rather than throwing - still playable, just not the improved
        // "always outruns the ship" behavior until aimAnchor is pointed at
        // the right object.
        shipRigidbody = aimAnchor.GetComponent<Rigidbody>();
        if (shipRigidbody == null)
        {
            Debug.LogWarning($"[LaserSpawner] Couldn't find a Rigidbody on aimAnchor ('{aimAnchor.name}') - lasers will launch at a fixed speed instead of inheriting the ship's velocity, which means the ship could catch up to its own shots again if it ever moves fast enough.", this);
        }

        // Same idea as the aimAnchor fallback above: rather than throwing a
        // null-reference exception the first time somebody clicks fire, we
        // fall back to firing from this object's own position so the game
        // stays playable while you go fix the Inspector setup. If you're
        // seeing this warning, it almost always means the spawnPoints list
        // is empty - open the Inspector and drag in at least one muzzle
        // Transform.
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"[LaserSpawner] No spawnPoints assigned on '{name}' - falling back to firing from this object's own position. Assign one Transform per barrel in the Inspector (two, for a twin-cannon look).", this);
            spawnPoints = new Transform[] { transform };
        }
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }

    private void Update()
    {
        // Recomputed every frame - not just when firing - so the crosshair
        // UI has a fresh, smooth position to follow each frame rather than
        // only updating in the instant a shot is fired.
        CurrentAimPoint = ComputeAimPoint();

        // WasPressedThisFrame() is the new Input System's equivalent of the
        // old GetMouseButtonDown - true for exactly one frame, the frame
        // the button/touch/gamepad button was first pressed, regardless of
        // which of those three actually triggered it.
        if (controls.Gameplay.Fire.WasPressedThisFrame())
        {
            if (GameManager.gmInstance.laserCount <= 0)
            {
                AudioSource.PlayClipAtPoint(noAmmoSound, transform.position);
                return;
            }

            FireLaser(CurrentAimPoint);

            // Spends exactly ONE shot of ammo for this click, no matter how
            // many barrels FireLaser() just fired from - see GameManager.cs
            // for that method. This is what keeps "twin cannons" reading as
            // a single shot instead of silently charging double.
            GameManager.gmInstance.UpdateLaserCount();
        }
    }

    // Works out where the crosshair should currently be pointing, in world
    // space: a spot out ahead of the ship, unless something solid is
    // actually in the way, in which case we aim precisely at that instead.
    private Vector3 ComputeAimPoint()
    {
        Vector3 desiredPoint = aimAnchor.position + Vector3.forward * aimLeadDistance;

        Vector3 camPos = Camera.main.transform.position;
        Vector3 direction = (desiredPoint - camPos).normalized;
        Ray aimRay = new Ray(camPos, direction);

        if (Physics.Raycast(aimRay, out RaycastHit hit, maxAimDistance, aimLayerMask))
        {
            return hit.point;
        }

        // Nothing in the way - just use the plain "ahead of the ship" point
        // rather than the raycast's fallback distance, so the crosshair
        // sits at a sensible, consistent distance instead of way off in
        // the distance whenever nothing happens to be hit.
        return desiredPoint;
    }

    // One "shot" from the player's perspective - one sound, one ammo cost -
    // that actually spawns a projectile out of every barrel in
    // spawnPoints. All of them are aimed at the same point, so the
    // projectiles converge on whatever's under the crosshair rather than
    // firing dead straight out of each barrel (which would make them
    // visibly diverge the farther out they travel).
    private void FireLaser(Vector3 aimPoint)
    {
        AudioSource.PlayClipAtPoint(laserSound, transform.position);

        foreach (Transform point in spawnPoints)
        {
            // Guards against a gap left in the array in the Inspector
            // (e.g. size set to 2 but only one slot actually filled in) -
            // skips it rather than throwing a null-reference exception and
            // silently eating every barrel after the empty one.
            if (point == null)
            {
                continue;
            }

            FireFromPoint(point, aimPoint);
        }
    }

    // The actual "spawn one projectile and send it flying" logic, pulled
    // out into its own method so FireLaser() above can just call it once
    // per barrel instead of duplicating this block in a loop.
    private void FireFromPoint(Transform point, Vector3 aimPoint)
    {
        // CHANGED from Instantiate() to ObjectPoolManager - a shot fires
        // this often (every single click), so it's exactly the kind of
        // thing pooling is meant for: reusing a laser sphere instead of
        // allocating and later garbage-collecting a brand new one every
        // time the player fires.
        GameObject sphere = ObjectPoolManager.instance.Spawn(spherePrefab, point.position, Quaternion.identity);
        Rigidbody sphereRb = sphere.GetComponent<Rigidbody>();

        Vector3 aimDirection = (aimPoint - point.position).normalized;

        // This used to be a fixed AddForce(..., ForceMode.Impulse) - which
        // gives every shot the SAME exit speed (shootForce divided by the
        // projectile's own mass) no matter how fast the ship firing it is
        // already moving. That's exactly why the ship could catch up to
        // its own lasers: once the ship's own top speed got close to that
        // fixed exit speed, there was barely any gap left to close.
        //
        // Setting velocity directly instead - as the ship's CURRENT
        // velocity plus shootForce worth of extra speed in the aim
        // direction - means a shot is always at least "shootForce" units
        // per second faster than the ship that just fired it, permanently,
        // regardless of how fast the ship speeds up in the future (say,
        // once forwardSpeed gets tuned higher, or once the "world rushes
        // toward the player" effect goes in). The `?? Vector3.zero` covers
        // the case where shipRigidbody wasn't found in Awake() - it just
        // falls back to the old fixed-speed behavior rather than crashing.
        Vector3 inheritedVelocity = shipRigidbody != null ? shipRigidbody.velocity : Vector3.zero;
        sphereRb.velocity = inheritedVelocity + aimDirection * shootForce;

        StartCoroutine(DestroySphere(sphere, 1f));
    }

    private IEnumerator DestroySphere(GameObject sphere, float delay)
    {
        yield return new WaitForSeconds(delay);

        // This is a SAFETY NET for shots that never hit anything and just
        // fly off into empty space - without it, those would stay active
        // forever. But a shot that DOES hit something gets released
        // immediately over on DeathByCollision (assuming that's on the
        // laser prefab, which explains the exact symptoms this was
        // fixing) - in that case, by the time this timer runs out a
        // second later, the sphere has already been released and is
        // sitting back in its pool as a spare for some OTHER shot to
        // reuse. IsActive() is what lets this timer tell the difference
        // and skip cleanly in that case, instead of trying to release the
        // same object a second time (which, before this fix, was
        // corrupting the pool and causing MissingReferenceExceptions on
        // later shots).
        if (ObjectPoolManager.instance.IsActive(sphere))
        {
            ObjectPoolManager.instance.Release(sphere);
        }
    }
}