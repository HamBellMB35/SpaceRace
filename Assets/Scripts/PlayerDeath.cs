using UnityEngine;

/// <summary>Ends the game when the player collides with an obstacle.</summary>
/// <remarks>
/// The public method is named RestartGame (rather than something like
/// TriggerGameOver) because Assets/Unity UI Samples/Scripts/ApplicationManager.cs
/// calls it directly by name — renaming it would break that script.
/// </remarks>
public class PlayerDeath : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        GameManager.gmInstance.EndGame();
    }
}