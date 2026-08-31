using System.Collections;
using UnityEngine;

/// <summary>Spends a life when the player collides with an obstacle.</summary>
/// <remarks>
/// The public method is named RestartGame (rather than something like
/// TriggerGameOver) because Assets/Unity UI Samples/Scripts/ApplicationManager.cs
/// calls it directly by name — renaming it would break that script.
///
/// CHANGED: this used to call GameManager.EndGame() directly, ending the
/// run on the very first obstacle hit. Now it calls GameManager.LoseLife()
/// instead - obstacles cost a life the same way going out of bounds does,
/// and GameManager.LoseLife() itself is the one that decides whether that
/// means "respawn and keep going" or "actually game over," depending on
/// how many lives are left. This method didn't need to change much to make
/// that happen - it's really just a one-line swap - but see the
/// invulnerability addition below for why that swap isn't QUITE as simple
/// as it first looks.
/// </remarks>
public class PlayerDeath : MonoBehaviour
{
    // Once a life is lost, RespawnSequence resets the ship back to the
    // middle of the track and grants a short invulnerability window before
    // handing control back. Without that window, a ship respawning at
    // roughly the same Z position it just died at could immediately
    // overlap the SAME obstacle (or another one right next to it) the
    // instant gameplay resumes, chain-losing a second life before the
    // player has even had a chance to react. This flag is what lets
    // RespawnSequence say "ignore obstacle hits for a moment" during that
    // window.
    private bool isInvulnerable;

    private void OnTriggerEnter(Collider other)
    {
        if (isInvulnerable)
        {
            return;
        }

        if (other.CompareTag("Obstacle"))
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        GameManager.gmInstance.LoseLife();
    }

    /// <summary>
    /// Called by RespawnSequence right as gameplay resumes after a
    /// respawn, so the ship can't immediately lose another life to the
    /// same obstacle (or a nearby one) it just respawned next to. Uses
    /// WaitForSecondsRealtime rather than WaitForSeconds specifically so
    /// this timer behaves predictably regardless of whatever Time.timeScale
    /// happens to be doing around the same moment.
    /// </summary>
    public void GrantInvulnerability(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(InvulnerabilityTimer(duration));
    }

    private IEnumerator InvulnerabilityTimer(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSecondsRealtime(duration);
        isInvulnerable = false;
    }
}