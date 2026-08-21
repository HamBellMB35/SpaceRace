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
public class DeathByCollision : MonoBehaviour
{
    public GameObject explosionPrefab;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Barrier"))
        {
            return;
        }

        if (!ObjectPoolManager.instance.IsActive(gameObject))
        {
            // Already died once this frame/moment (see the class comment
            // above) - skip entirely rather than double up on the
            // explosion and the release.
            return;
        }

        GameObject explosion = ObjectPoolManager.instance.Spawn(explosionPrefab, transform.position, Quaternion.identity);
        ObjectPoolManager.instance.ReleaseAfterDelay(explosion, 1f);
        ObjectPoolManager.instance.Release(gameObject);
    }
}