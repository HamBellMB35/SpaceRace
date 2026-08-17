using System.Collections;
using UnityEngine;

// Fires laser projectiles at a target and handles the ammo check before
// doing so. Ammo itself lives entirely in GameManager.laserCount now (see
// the comment history in GameManager.cs / the AddLasers method for why we
// consolidated to a single counter instead of this script keeping its own).
//
// Firing input now comes through the new Input System's "Fire" action
// (bound to left mouse click, a gamepad face button, and a touch press -
// see Assets/Input/PlayerControls.inputactions) instead of the old
// Input.GetMouseButtonDown(0). Functionally this behaves the same as
// before on PC, it's just no longer hardcoded to "mouse click specifically"
// - which matters once this actually needs to work on a phone with no
// mouse at all.
public class LaserSpawner : MonoBehaviour
{
    public GameObject spherePrefab;
    public float shootForce = 10f;
    public Transform shootTarget;
    public Transform spawnPoint;
    public AudioClip laserSound;
    public AudioClip noAmmoSound;

    // Same generated-class pattern as PlayerMovement - each script that
    // needs input gets its own PlayerControls instance and manages its own
    // Enable/Disable lifecycle. They don't need to know about each other;
    // the Input System handles multiple listeners on the same actions fine.
    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
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

            FireLaser(shootTarget);

            // Spends one shot of ammo AND updates the on-screen laser count
            // text in one call - see GameManager.cs for that method.
            GameManager.gmInstance.UpdateLaserCount();
        }
    }

    private void FireLaser(Transform target)
    {
        AudioSource.PlayClipAtPoint(laserSound, transform.position);

        // Create a sphere at the spawn point and shoot it towards the target
        GameObject sphere = Instantiate(spherePrefab, spawnPoint.position, Quaternion.identity);

        sphere.GetComponent<Rigidbody>().AddForce(
            (target.position - spawnPoint.position).normalized * shootForce,
            ForceMode.Impulse);

        StartCoroutine(DestroySphere(sphere, 1f));
    }

    private IEnumerator DestroySphere(GameObject sphere, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(sphere);
    }
}