using UnityEngine;

/// <summary>Plays a sound and awards score when the player passes through this trigger.</summary>
public class ScoreUpdater : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip clip;
    public float volume = 0.5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            audioSource.PlayOneShot(clip, volume);
            UpdateScoreText();
        }
    }

    private void UpdateScoreText()
    {
        GameManager.gmInstance.UpdateScore();
    }
}
