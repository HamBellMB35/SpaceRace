using UnityEngine;

/// <summary>UI hook for ending the current game (e.g. from a menu button).</summary>
public class SceneLoader : MonoBehaviour
{
    public void RestartGame()
    {
        GameManager.gmInstance.EndGame();
    }
}
