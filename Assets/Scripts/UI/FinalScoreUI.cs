using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

// Waits on the game-over screen for literally any input - any key, mouse
// click, gamepad button, or touch - then reloads the game.
//
// The old Input.anyKeyDown only ever covered keyboard and mouse, which is
// exactly the kind of thing that quietly breaks once this needs to run on
// a phone with no keyboard at all. The new Input System has a purpose-built
// answer for "the player pressed something, I don't care what device or
// which specific button": InputSystem.onAnyButtonPress. It's an observable
// you subscribe to rather than something you poll in Update() - you get a
// callback the instant any button-like control on any connected device
// gets pressed, covering keyboard, mouse, gamepad, and touch all in one
// place without needing to check each device separately.
public class FinalScoreUI : MonoBehaviour
{
    // Subscribing to onAnyButtonPress hands back an IDisposable - calling
    // Dispose() on it is how you unsubscribe. We hang onto it here so
    // OnDisable can clean up properly instead of leaving a dangling
    // subscription that keeps firing (and keeps a reference to this
    // destroyed object alive) after this screen is gone.
    private IDisposable anyButtonPressListener;

    private void OnEnable()
    {
        anyButtonPressListener = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
    }

    private void OnDisable()
    {
        anyButtonPressListener?.Dispose();
        anyButtonPressListener = null;
    }

    private void OnAnyButtonPressed(InputControl control)
    {
        // Loading by NAME instead of a hardcoded build index (1) - the
        // exact same fix, and the exact same reason, as GameManager's
        // LoadGame() method: a bare number like SceneManager.LoadScene(1)
        // just means "whichever scene currently sits in that slot in
        // File > Build Settings," which silently changes meaning any time
        // scenes get reordered there. This used to correctly point at the
        // menu scene back when UI_v2 was at index 1, but after moving
        // UI_v2 to the top (index 0) so it plays first, index 1 became
        // GamePlayScene instead - which is exactly why "press any key" on
        // the game-over screen started restarting the run instead of
        // returning to the main menu. Loading "UI_v2" by name means this
        // keeps working correctly no matter what order the scenes are in.
        SceneManager.LoadScene("UI_v2");
    }
}