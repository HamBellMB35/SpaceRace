using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// A central, reusable object pool for anything in the game that gets
// created and destroyed over and over during a run - laser shots,
// explosions, enemies, obstacles, and whole map chunks. This wraps
// Unity's own BUILT-IN pooling API (UnityEngine.Pool.ObjectPool<T>, added
// in Unity 2021.1) rather than a hand-rolled one - it already handles all
// the fiddly bookkeeping (tracking spares, growing/shrinking, calling your
// callbacks at the right moments); this script just wires up one pool per
// prefab and gives the rest of the project a simple Spawn()/Release() pair
// to call instead of Instantiate()/Destroy().
//
// WHY POOLING AT ALL: every single Instantiate() and Destroy() call is
// more expensive than it looks - allocating memory, running the whole
// component-setup pipeline, and (for Destroy) leaving garbage behind for
// Unity's garbage collector to clean up later. In an endless runner,
// you're doing this constantly - every laser shot, every obstacle
// explosion, every new map chunk rolling in. On a phone especially, all
// those repeated allocations cause visible hitches ("GC spikes") as the
// garbage collector periodically has to stop everything to clean up after
// itself. Pooling sidesteps almost all of that: an object that "dies"
// doesn't actually get destroyed anymore, it just gets hidden and parked,
// ready to be repositioned and reused the next time something needs
// another one just like it - no new allocation, no garbage.
//
// HOW TO USE THIS FROM ANOTHER SCRIPT:
//   Instead of:  Instantiate(somePrefab, position, rotation)
//   Do:          ObjectPoolManager.instance.Spawn(somePrefab, position, rotation)
//
//   Instead of:  Destroy(someInstance)
//   Do:          ObjectPoolManager.instance.Release(someInstance)
//
// IMPORTANT FOR ANY SCRIPT ATTACHED TO A POOLED PREFAB: Start() only ever
// runs ONCE per object, on the very first frame it's ever created - NOT
// again every time a pooled instance gets reused later. Any script that
// needs to reset itself every time its object comes back into play
// (picking a new random target, restarting a timer, etc.) needs that logic
// in OnEnable() instead, since OnEnable() reliably fires every single time
// an object is reactivated, including every reuse. Randomizer.cs, Enemy.cs,
// and SelfDestruct.cs were all changed from Start() to OnEnable() for
// exactly this reason as part of this same pooling pass.
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager instance;

    [Tooltip("How many SPARE (inactive, ready-to-reuse) instances of EACH prefab the pool starts out holding onto. This does NOT limit how many can be ACTIVE at the same time - if gameplay needs more than this, extra ones simply get created as needed. It's just a head start so the pool doesn't need to grow from scratch during the first few seconds of play.")]
    public int defaultPoolCapacity = 20;

    [Tooltip("The most SPARE (inactive) instances of any ONE prefab the pool will hold onto before it starts destroying the extras instead of keeping them. This doesn't cap how many can be ACTIVE at once either - it only controls how many unused spares get kept in reserve once things quiet back down, as a safety net against something like a runaway spawn loop eating unlimited memory.")]
    public int maxPoolSizePerPrefab = 200;

    // One separate pool per distinct prefab - a laser shot and an
    // explosion are completely different objects, so each prefab gets its
    // own pool of spares. A prefab's pool is created lazily, the first
    // time that specific prefab is ever asked for via Spawn().
    private readonly Dictionary<GameObject, ObjectPool<GameObject>> pools = new Dictionary<GameObject, ObjectPool<GameObject>>();

    // Every currently-ACTIVE pooled instance is recorded here, mapping it
    // back to whichever prefab (and therefore which pool) it came from.
    // That's what lets Release() below work from just the instance alone -
    // whatever's calling Release() doesn't need to separately remember and
    // pass back "and this one came from THIS prefab" every time.
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        // Same duplicate-singleton safety pattern as GameManager.cs - if
        // two of these somehow end up in the scene at once, keep the
        // first one and clean up the extra, rather than having two
        // competing pool managers silently fighting over the same prefabs.
        if (instance != null && instance != this)
        {
            Debug.LogError($"[ObjectPoolManager] Found a second ObjectPoolManager ('{name}') in the scene - '{instance.name}' already claimed the instance slot. Destroying this duplicate.", this);
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    /// <summary>
    /// Gets a ready-to-use instance of prefab (creating a brand new one
    /// only if every existing spare is already active elsewhere), places
    /// it at position and rotation, and activates it. Use this everywhere
    /// you would have previously used Instantiate() for something that
    /// gets created and destroyed repeatedly during gameplay.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[ObjectPoolManager] Spawn() was called with a null prefab - nothing to spawn.", this);
            return null;
        }

        ObjectPool<GameObject> pool = GetOrCreatePool(prefab);
        GameObject instance = pool.Get();

        // Position and rotate BEFORE activating - not after. This
        // ordering matters more than it looks: activating an object
        // (SetActive(true)) is what fires OnEnable() on every component
        // attached to it. If we activated first and moved it second, any
        // reused script that reads its own position during OnEnable()
        // (say, to spawn something else relative to itself, like
        // Randomizer.cs does) would briefly see its OLD position left over
        // from the last time this instance was used, not its new one.
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);

        instanceToPrefab[instance] = prefab;
        return instance;
    }

    /// <summary>
    /// True if instance is a pooled object that's currently active and
    /// tracked by this manager - i.e. it's safe to Release() right now.
    /// Call this before Release() whenever the SAME object might
    /// legitimately get released from more than one place - for example,
    /// a laser that gets released immediately by DeathByCollision when it
    /// hits something, AND separately by a fallback timer in case it never
    /// hits anything at all. Whichever one runs first should actually
    /// release it; the other should just skip quietly instead of trying to
    /// release something that's already back in the pool.
    /// </summary>
    public bool IsActive(GameObject instance)
    {
        return instance != null && instanceToPrefab.ContainsKey(instance);
    }

    /// <summary>
    /// Returns a previously-Spawn()ed instance to its pool - deactivating
    /// it and making it available for reuse - instead of destroying it.
    /// Use this everywhere you would have previously used Destroy() on
    /// something that came from Spawn() above.
    /// </summary>
    public void Release(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!instanceToPrefab.TryGetValue(instance, out GameObject prefab))
        {
            // This object was never handed out by Spawn() in the first
            // place, or - far more commonly - it already got released once
            // from somewhere else and is currently sitting safely back in
            // its pool as a spare.
            //
            // IMPORTANT: this used to fall back to Destroy()-ing the
            // object directly here, on the theory that it must be an
            // orphan. That was actually a dangerous assumption - an
            // object that's "already released" is usually NOT an orphan,
            // it's a legitimate spare the pool still owns and expects to
            // hand out again later. Destroying it out from under the pool
            // corrupts that prefab's entire pool: the next Spawn() call
            // for that prefab can hand back a reference to an object that
            // no longer actually exists, which shows up as a
            // MissingReferenceException the moment anything touches it.
            // Simply doing nothing here is far safer - at worst, this
            // logs a warning and moves on; it never breaks a pool that's
            // otherwise working correctly. Prefer IsActive() (above) to
            // avoid even calling Release() a second time in the first
            // place, wherever that's possible.
            Debug.LogWarning($"[ObjectPoolManager] Release() was called on '{instance.name}', which this manager doesn't recognize as currently active (never spawned through it, or already released) - ignoring it rather than risk corrupting its pool.", instance);
            return;
        }

        instanceToPrefab.Remove(instance);
        pools[prefab].Release(instance);
    }

    /// <summary>
    /// Releases instance back to its pool after delay seconds - equivalent
    /// to a script starting its own coroutine that waits, then calls
    /// Release() itself, EXCEPT this one's coroutine runs on
    /// ObjectPoolManager (a persistent object that's never deactivated or
    /// destroyed), not on whatever script originally asked for it.
    ///
    /// That distinction matters whenever the object doing the asking might
    /// itself be deactivated or released BEFORE the delay is up - and
    /// that's exactly DeathByCollision.cs's situation: it releases itself
    /// (the obstacle/enemy that died) in the very same moment it asks for
    /// the explosion to be cleaned up a second later. A coroutine hosted
    /// directly on that dying object would get silently killed the instant
    /// it deactivates, meaning the explosion would never actually get
    /// released at all - a real, easy-to-miss leak. Hosting the wait here
    /// instead guarantees it always runs to completion regardless of what
    /// happens to whoever asked for it.
    /// </summary>
    public void ReleaseAfterDelay(GameObject instance, float delay)
    {
        StartCoroutine(ReleaseAfterDelayRoutine(instance, delay));
    }

    private IEnumerator ReleaseAfterDelayRoutine(GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        Release(instance);
    }

    private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out ObjectPool<GameObject> existingPool))
        {
            return existingPool;
        }

        ObjectPool<GameObject> newPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject newInstance = Instantiate(prefab);

                // Created INACTIVE on purpose. Spawn() above is the one
                // that activates an instance, and it does so only AFTER
                // positioning it correctly. If a brand new instance came
                // out of Instantiate() already active (which is the
                // default), its OnEnable() would fire immediately at the
                // prefab's default position, before Spawn() ever got the
                // chance to move it to where it's actually supposed to go.
                newInstance.SetActive(false);
                return newInstance;
            },
            actionOnGet: null, // deliberately does nothing - Spawn() above handles activating, AFTER positioning (see the comment there)
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            collectionCheck: true, // catches accidentally Release()-ing the same instance twice, with a clear error instead of silently corrupting the pool
            defaultCapacity: defaultPoolCapacity,
            maxSize: maxPoolSizePerPrefab);

        pools[prefab] = newPool;
        return newPool;
    }
}