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
        SceneManager.LoadScene(1);
    }
}