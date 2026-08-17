using UnityEngine;

/// <summary>Destroys anything that enters this trigger, except objects on the ignored layer.</summary>
public class EnemyDestroyer : MonoBehaviour
{
    public LayerMask ignoredLayer;

    private void OnTriggerEnter(Collider other)
    {
        if ((ignoredLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            return;
        }

        Destroy(other.gameObject);
    }
}
