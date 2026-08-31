using UnityEngine;

// Attach this to whichever "barrier" objects in your level should grant the
// player bonus laser ammo when they fly through them. This is a standalone
// component, not tied to the "Barrier" tag that DeathByCollision.cs checks
// elsewhere for a completely different purpose (deciding what does/doesn't
// destroy a projectile on contact) - so attaching this script to an object
// is what opts it into "flying through this grants lasers," regardless of
// what tag that object happens to have. That keeps this feature isolated
// from that other tag-based logic instead of accidentally tangling the two
// together.
//
// Requires: the object this is attached to needs a Collider with "Is
// Trigger" checked (otherwise OnTriggerEnter below never fires), and the
// player ship needs to be tagged "Player" - which it already should be,
// since ScoreUpdater.cs elsewhere in the project relies on that same tag.
public class LaserBarrier : MonoBehaviour
{
    [Tooltip("How many bonus laser shots the player gets for flying through this specific barrier. Editable per-instance, so different barriers can be worth different amounts if you want that later - a big gate could be worth more than a small one, for example.")]
    public int laserBonusAmount = 5;

    private void OnTriggerEnter(Collider other)
    {
        // Only react to the player - we don't want, say, an enemy or a
        // stray obstacle drifting through this trigger to also hand out
        // free ammo.
        if (other.CompareTag("Player"))
        {
            GameManager.gmInstance.AddLasers(laserBonusAmount);
        }
    }
}