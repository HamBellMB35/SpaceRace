using UnityEngine;

/// <summary>Releases this object back to its pool and spawns an explosion when it hits anything but the barrier.</summary>
/// <remarks>
/// CHANGED for object pooling: this used to Instantiate() the explosion and
/// Destroy() both itself and the explosion outright. Now it Spawn()s and
/// Release()s through ObjectPoolManager instead - same visible behavior
/// (something explodes, both objects go away), but neither one is actually
/// destroyed and recreated from scratch anymore, just recycled. This runs
/// every time ANY obstacle or enemy dies, which happens constantly during a
/// run, so it's a prime candidate for pooling.
///
/// The explosion's own cleanup now goes through
/// ObjectPoolManager.ReleaseAfterDelay() instead of a coroutine on THIS
/// object - see that method's comment for why that swap matters here
/// specifically (this object releases itself in the same breath it asks
/// for the explosion's delayed release, so a coroutine hosted here would
/// get killed before it ever finished waiting).
///
/// Also guards against being triggered more than once - the twin-cannon
/// laser setup fires both barrels at once, aimed at the same point, so
/// it's completely normal for BOTH shots to hit the exact same
/// obstacle/enemy in the same instant. Each shot's collider fires its own
/// separate OnTriggerEnter here, independently of the other one. Without
/// the IsActive() check below, the first hit would correctly kill this
/// object, and the second hit (processed a moment later, before Unity's
/// finished handling everything else for that physics step) would try to
/// do the exact same thing all over again to something that's already
/// gone - spawning a pointless second explosion on top of the first, and
/// trying to release an object that's already been released.
/// </remarks>
// REWRITTEN: this script was trying to do two genuinely different jobs at
// once, using one single "ignore everything except a growing list of
// exceptions" rule for both - and that's exactly why we kept finding new
// gaps (map geometry, the despawn zone, other obstacles...). An OBSTACLE
// should only ever be killed by a laser or the player - nothing else.
// A LASER, on the other hand, genuinely SHOULD explode from touching
// almost anything solid (that's the whole point of a projectile), except
// for a few specific pass-through zones. Those are opposite rules, so this
// now checks which kind of object it's sitting on (via its own tag) and
// applies the correct rule for that role, instead of one shared list
// trying to cover both at once.
public class DeathByCollision : MonoBehaviour
{
    public GameObject explosionPrefab;

    [Tooltip("Layers that should NEVER count as a real hit, no matter what they're tagged - most importantly your map/track geometry layer. Only used for the LASER'S rule below (obstacles already ignore everything except a laser or the player, so this doesn't need to include anything obstacle-specific).")]
    public LayerMask ignoredLayer;

    private void OnTriggerEnter(Collider other)
    {
        if (!ObjectPoolManager.instance.IsActive(gameObject))
        {
            // Already died once this frame/moment - the twin-cannon laser
            // setup fires both barrels at once, aimed at the same point,
            // so it's completely normal for both shots to hit the exact
            // same obstacle in the same instant. Skip entirely rather
            // than double up on the explosion and the release.
            return;
        }

        // gameObject here is THIS object - the one DeathByCollision is
        // actually sitting on - not "other". Checking ITS OWN tag is what
        // lets one shared script apply two different rules depending on
        // whether it's living on an obstacle/enemy or on a laser.
        if (gameObject.CompareTag("Obstacle"))
        {
            if (!IsLegitimateObstacleKiller(other))
            {
                return;
            }
        }
        else
        {
            if (!IsLegitimateProjectileHit(other))
            {
                return;
            }
        }

        GameObject explosion = ObjectPoolManager.instance.Spawn(explosionPrefab, transform.position, Quaternion.identity);
        ObjectPoolManager.instance.ReleaseAfterDelay(explosion, 1f);
        ObjectPoolManager.instance.Release(gameObject);
    }

    // The rule for something tagged Obstacle (an asteroid, mine, or enemy
    // ship): ONLY a laser or the player actually counts as a real kill.
    // This is a strict ALLOW list rather than a growing ignore list - so
    // it automatically, correctly ignores literally everything else in
    // one go: other obstacles bumping into it, the map/track geometry, the
    // despawn zone behind the player, a ProximityWoosh near-miss zone,
    // anything - without needing a separate special-case check for each
    // one, and without any risk of a new kind of object slipping through
    // later the way the old ignore-list kept doing.
    private bool IsLegitimateObstacleKiller(Collider other)
    {
        bool isPlayer = other.CompareTag("Player") || other.transform.root.CompareTag("Player");
        bool isLaser = other.CompareTag("Laser") || other.transform.root.CompareTag("Laser");
        return isPlayer || isLaser;
    }

    // The rule for anything ELSE with this script on it - in practice,
    // that's the laser sphere prefab. A laser genuinely should explode
    // from touching almost any solid obstacle or piece of geometry, so
    // this keeps the original "ignore a short specific list, treat
    // everything else as a real hit" behavior, rather than the strict
    // allow-list above (which would incorrectly stop a laser from ever
    // detonating against an obstacle, since an obstacle isn't the player
    // or another laser).
    private bool IsLegitimateProjectileHit(Collider other)
    {
        if ((ignoredLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            return false;
        }

        if (other.CompareTag("Barrier"))
        {
            return false;
        }

        // A ProximityWoosh "near miss" zone. Those trigger colliders are
        // DELIBERATELY wider than the object's real collision shape (see
        // ProximityWoosh.cs's own setup comment for why) - without this,
        // a laser would detonate the instant it enters that wider zone,
        // well before it ever actually reaches the real obstacle
        // underneath it.
        if (other.GetComponent<ProximityWoosh>() != null)
        {
            return false;
        }

        // EnemyDestroyer's own cleanup/despawn zone - the trigger sitting
        // behind the player that quietly recycles obstacles once they're
        // out of view. A laser passing through that same zone (say, one
        // that missed everything and flew off past the player) shouldn't
        // count as "hitting" anything either.
        if (other.GetComponentInParent<EnemyDestroyer>() != null)
        {
            return false;
        }

        return true;
    }
}