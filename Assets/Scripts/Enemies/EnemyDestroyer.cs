using UnityEngine;

/// <summary>Releases anything that enters this trigger back to its pool (or destroys it, if it was never a pooled object), except objects on the ignored layer.</summary>
/// <remarks>
/// CHANGED for object pooling. This is the generic "behind the camera"
/// cleanup trigger, so it's whatever actually catches map chunks,
/// obstacles, and enemies once they've scrolled far enough past the
/// player.
///
/// Uses other.transform.root instead of other.gameObject: the collider
/// that actually touches this trigger might live on a CHILD of a pooled
/// object - a chunk's own wall/boundary collider, say - rather than on
/// that pooled object's own top-level GameObject. ObjectPoolManager only
/// ever tracks the top-level root instances it actually handed out via
/// Spawn(), so checking .root here is what correctly finds "the real
/// pooled thing this belongs to," instead of just the one small child
/// piece that happened to be what touched the trigger.
///
/// Also now falls back to a plain Destroy() for anything that ISN'T a
/// pooled object at all - like a lane delimiter, which was never part of
/// this pooling pass. Without that fallback, this script would just do
/// nothing for anything it doesn't recognize (safe, but those objects
/// would then never get cleaned up at all) - this restores exactly the
/// original pre-pooling behavior for anything genuinely outside the pool
/// system.
/// </remarks>
public class EnemyDestroyer : MonoBehaviour
{
    public LayerMask ignoredLayer;

    private void OnTriggerEnter(Collider other)
    {
        if ((ignoredLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            return;
        }

        GameObject rootObject = other.transform.root.gameObject;

        if (ObjectPoolManager.instance.IsActive(rootObject))
        {
            ObjectPoolManager.instance.Release(rootObject);
        }
        else
        {
            Destroy(rootObject);
        }
    }
}