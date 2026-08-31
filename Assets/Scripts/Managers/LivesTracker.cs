// Same idea again - a small, plain class owning just the lives NUMBER and
// the one rule that actually matters about it: whether spending a life
// just ended the run. GameManager still decides WHAT TO DO about that
// (show the game-over screen, update the life icons, and so on) - this
// class only answers the yes/no question of whether it happened.
public class LivesTracker
{
    public int Remaining { get; private set; }

    public LivesTracker(int startingLives)
    {
        Remaining = startingLives;
    }

    // Lets the current count be overridden directly, separate from the
    // constructor. This exists mainly so GameManager's own public `lives`
    // property (kept around purely so nothing else in the project needs to
    // change) can still both read AND write the live value at any time,
    // not just set it once up front.
    public void SetRemaining(int value)
    {
        Remaining = value;
    }

    /// <summary>Spends one life and reports whether that was the last one (true = game over).</summary>
    public bool LoseOne()
    {
        Remaining--;
        return Remaining <= 0;
    }
}
