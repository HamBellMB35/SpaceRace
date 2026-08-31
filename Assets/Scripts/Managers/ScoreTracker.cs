// A tiny, plain class - deliberately NOT a MonoBehaviour - that owns
// exactly one thing: what the player's score is, and the one rule for how
// it goes up. It has zero knowledge of UI Text, GameObjects, or anything
// else Unity-scene-related, on purpose. That split is what makes this
// trivially testable: an EditMode test can do `new ScoreTracker()` and
// check the math directly, with no scene, no Play Mode, and no GameObject
// required at all - something that was flatly impossible while this logic
// lived tangled up inside GameManager alongside score TEXT, UI panels, and
// singleton bookkeeping.
public class ScoreTracker
{
    public int Score { get; private set; }

    // How many points one score trigger is worth. Kept as a real,
    // constructor-settable value here - not a bare "+= 100" magic number
    // buried inside a much bigger method - so the one number that actually
    // defines "how much is a score trigger worth" lives in exactly one
    // obvious, named place.
    private readonly int pointsPerTrigger;

    public ScoreTracker(int pointsPerTrigger = 100)
    {
        this.pointsPerTrigger = pointsPerTrigger;
    }

    /// <summary>Awards one trigger's worth of points and returns the new running total.</summary>
    public int AddPoints()
    {
        Score += pointsPerTrigger;
        return Score;
    }
}
