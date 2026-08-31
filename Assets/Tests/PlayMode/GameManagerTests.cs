using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// These are PLAYMODE tests, not EditMode ones - meaning they run inside an
// actual (temporary, empty) scene with real MonoBehaviour lifecycle
// methods firing normally, unlike EditMode tests, which run with no scene
// at all and skip Awake/Start/Update entirely. That distinction matters a
// lot here, because GameManager relies on its own Awake() method to set up
// the static gmInstance singleton - an EditMode test would never see that
// happen.
//
// Notice these tests don't wire up ANY of GameManager's optional UI
// fields (scoreText, livesDisplay, respawnSequence, and so on) before
// calling LoseLife(). That's deliberate, not an oversight: GameManager was
// specifically written so every one of those references is optional and
// null-checked right where it's used (see GameManager.cs's own class
// comment for the full reasoning) - which is exactly what makes a small,
// focused test like this possible at all, instead of needing to build out
// an entire scene's worth of UI just to test one rule about lives.
public class GameManagerTests
{
    // [UnityTest] (instead of a plain [Test]) is what lets a test spread
    // across multiple frames using "yield return", the same way a
    // coroutine does - a plain [Test] runs instantly in one shot and can't
    // wait for a frame to actually pass, which matters here since
    // GameObject.AddComponent immediately runs Awake() but this test still
    // wants to mirror how the real game behaves frame-to-frame.
    [UnityTest]
    public IEnumerator LosingLastLife_EndsTheGame()
    {
        // Arrange: a bare GameManager on a throwaway GameObject, with
        // lives set to exactly 1 - one hit away from game over.
        GameObject gameManagerObject = new GameObject("TestGameManager");
        GameManager gameManager = gameManagerObject.AddComponent<GameManager>();
        gameManager.lives = 1;

        yield return null;

        // Act: spend the one remaining life.
        gameManager.LoseLife();

        // Assert: the run should be over - lives at exactly 0. If this
        // ever fails after a future change to GameManager, it means
        // something about the "0 lives = game over" rule broke.
        Assert.AreEqual(0, gameManager.lives, "Losing the last life should bring lives down to exactly 0.");

        // Cleanup: destroy the object (and along with it, GameManager's
        // static gmInstance reference) so this test doesn't leave anything
        // behind that could confuse the NEXT test that runs after it.
        Object.Destroy(gameManagerObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator LosingALife_WithLivesRemaining_DoesNotEndTheGame()
    {
        // Same idea, but starting with lives to spare - this is the
        // "mid-run, not game over yet" case, which is just as important to
        // check as the game-over case above. Without this test, a change
        // that accidentally made EVERY life loss end the game would still
        // pass the test above (since 1 life lost still correctly reaches
        // exactly 0) - this second test is what would actually catch that.
        GameObject gameManagerObject = new GameObject("TestGameManager");
        GameManager gameManager = gameManagerObject.AddComponent<GameManager>();
        gameManager.lives = 3;

        yield return null;

        gameManager.LoseLife();

        Assert.AreEqual(2, gameManager.lives, "Losing a life with lives remaining should only subtract exactly one.");

        Object.Destroy(gameManagerObject);
        yield return null;
    }
}
