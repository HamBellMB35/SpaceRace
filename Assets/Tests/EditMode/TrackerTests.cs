using NUnit.Framework;

// This is an EditMode test file, not a PlayMode one like GameManagerTests -
// notice there's no [UnityTest], no IEnumerator, no "yield return null"
// anywhere in here, and no GameObject ever gets created. That's not a
// style choice, it's the whole point: ScoreTracker, LaserAmmoTracker, and
// LivesTracker are plain C# classes with zero Unity dependencies, so
// testing them doesn't need a scene, doesn't need Play Mode to spin up,
// and doesn't need a single frame to pass. Unity's Test Runner runs these
// basically instantly, in the Editor, without ever pressing Play - that
// speed and simplicity is the concrete payoff of pulling this logic out
// of GameManager in the first place. Before the refactor, none of this
// would have been possible to test without a whole wired-up scene.
public class TrackerTests
{
    // --- ScoreTracker ---------------------------------------------------

    [Test]
    public void ScoreTracker_StartsAtZero()
    {
        var scoreTracker = new ScoreTracker();
        Assert.AreEqual(0, scoreTracker.Score, "A brand new ScoreTracker shouldn't have any points yet.");
    }

    [Test]
    public void ScoreTracker_AddPoints_UsesDefaultOneHundredPerTrigger()
    {
        var scoreTracker = new ScoreTracker();
        int newScore = scoreTracker.AddPoints();

        // AddPoints() both updates Score internally AND returns the new
        // total directly - this checks both of those actually agree with
        // each other, not just one or the other.
        Assert.AreEqual(100, newScore, "The return value from AddPoints() should be the new running total.");
        Assert.AreEqual(100, scoreTracker.Score, "Score itself should also reflect the same total.");
    }

    [Test]
    public void ScoreTracker_AddPoints_AccumulatesAcrossMultipleCalls()
    {
        var scoreTracker = new ScoreTracker();
        scoreTracker.AddPoints();
        scoreTracker.AddPoints();
        scoreTracker.AddPoints();

        Assert.AreEqual(300, scoreTracker.Score, "Three separate score triggers, 100 points each, should add up to 300 - not overwrite each other.");
    }

    [Test]
    public void ScoreTracker_RespectsACustomPointsPerTrigger()
    {
        // Confirms the pointsPerTrigger constructor argument is actually
        // wired up, rather than the class silently always using 100
        // regardless of what's passed in.
        var scoreTracker = new ScoreTracker(pointsPerTrigger: 25);
        scoreTracker.AddPoints();

        Assert.AreEqual(25, scoreTracker.Score, "A ScoreTracker built with a custom pointsPerTrigger should award that amount instead of the default 100.");
    }

    // --- LaserAmmoTracker ------------------------------------------------

    [Test]
    public void LaserAmmoTracker_StartsAtTheGivenCount()
    {
        var laserAmmoTracker = new LaserAmmoTracker(startingCount: 30);
        Assert.AreEqual(30, laserAmmoTracker.Count, "The starting ammo count passed into the constructor should be exactly what Count reports right away.");
    }

    [Test]
    public void LaserAmmoTracker_TrySpendOne_DecrementsCountAndReturnsTrue_WhenAmmoIsAvailable()
    {
        var laserAmmoTracker = new LaserAmmoTracker(startingCount: 3);
        bool spent = laserAmmoTracker.TrySpendOne();

        Assert.IsTrue(spent, "Spending one shot while ammo is available should report success.");
        Assert.AreEqual(2, laserAmmoTracker.Count, "Count should drop by exactly one, not more and not less.");
    }

    [Test]
    public void LaserAmmoTracker_TrySpendOne_FailsAndClampsAtZero_WhenAlreadyEmpty()
    {
        // This is the "don't let ammo go negative" guarantee - the whole
        // reason TrySpendOne returns a bool instead of just always
        // decrementing blindly.
        var laserAmmoTracker = new LaserAmmoTracker(startingCount: 0);
        bool spent = laserAmmoTracker.TrySpendOne();

        Assert.IsFalse(spent, "Trying to spend ammo with none left should report failure, not silently succeed.");
        Assert.AreEqual(0, laserAmmoTracker.Count, "Count should stay clamped at 0, never dip into negative numbers.");
    }

    [Test]
    public void LaserAmmoTracker_Add_GrantsBonusAmmoOnTopOfTheCurrentCount()
    {
        var laserAmmoTracker = new LaserAmmoTracker(startingCount: 10);
        laserAmmoTracker.Add(5);

        Assert.AreEqual(15, laserAmmoTracker.Count, "Add() should increase Count by exactly the amount passed in, on top of whatever was already there.");
    }

    // --- LivesTracker ------------------------------------------------

    [Test]
    public void LivesTracker_StartsAtTheGivenCount()
    {
        var livesTracker = new LivesTracker(startingLives: 3);
        Assert.AreEqual(3, livesTracker.Remaining, "The starting lives passed into the constructor should be exactly what Remaining reports right away.");
    }

    [Test]
    public void LivesTracker_LoseOne_WithLivesRemaining_DecrementsAndReportsNotGameOver()
    {
        var livesTracker = new LivesTracker(startingLives: 3);
        bool isGameOver = livesTracker.LoseOne();

        Assert.IsFalse(isGameOver, "Losing a life while more than one remains shouldn't be reported as game over.");
        Assert.AreEqual(2, livesTracker.Remaining, "Remaining should drop by exactly one.");
    }

    [Test]
    public void LivesTracker_LoseOne_OnTheLastLife_ReportsGameOver()
    {
        // This is the exact rule GameManager.LoseLife() leans on to decide
        // whether to call EndGame() or hand off to the respawn sequence -
        // so this one test is effectively locking in the single most
        // important behavior in the whole class.
        var livesTracker = new LivesTracker(startingLives: 1);
        bool isGameOver = livesTracker.LoseOne();

        Assert.IsTrue(isGameOver, "Losing the very last life should be reported as game over.");
        Assert.AreEqual(0, livesTracker.Remaining, "Remaining should land at exactly 0, not go negative.");
    }

    [Test]
    public void LivesTracker_SetRemaining_OverridesTheCurrentValueDirectly()
    {
        // This mirrors exactly what GameManager's public `lives` property
        // setter does, and what GameManagerTests.cs relies on when it does
        // `gameManager.lives = 1;` to set up a scenario before testing it.
        var livesTracker = new LivesTracker(startingLives: 3);
        livesTracker.SetRemaining(1);

        Assert.AreEqual(1, livesTracker.Remaining, "SetRemaining() should overwrite Remaining directly, independent of the constructor's starting value.");
    }
}
