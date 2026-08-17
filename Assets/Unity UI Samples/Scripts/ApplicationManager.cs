using UnityEngine;
using System.Collections;

public class ApplicationManager : MonoBehaviour {
	
	PlayerDeath playerDeath;

	private void Start()
	{
		Cursor.visible = true;
		playerDeath = FindAnyObjectByType<PlayerDeath>();
		if(playerDeath != null)
		{
			Debug.Log(" FOUND THE SCRIPT");
		}
	}
	public void Quit () 
	{
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#else
		Application.Quit();
		#endif
	}

	public void StartGame () 
	{
		
		playerDeath.RestartGame();
		
	}

	
}
