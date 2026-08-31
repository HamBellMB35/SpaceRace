using System.Collections;
using UnityEngine;

/// <summary>Releases this object back to its pool one second after it becomes active.</summary>
/// <remarks>
/// CHANGED for object pooling: Destroy() became Release(), and Start()
/// became OnEnable() - this script can end up on a pooled, reused prefab,
/// and Start() only ever fires once per object (its very first spawn),
/// while OnEnable() correctly fires every time, including every reuse.
/// Without that second change, an object using this script would
/// self-destruct-timer correctly exactly once ever, then never again on
/// any later reuse.
/// </remarks>
public class SelfDestruct : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(Destruct());
    }

    private IEnumerator Destruct()
    {
        yield return new WaitForSeconds(1f);
        ObjectPoolManager.instance.Release(gameObject);
    }
}