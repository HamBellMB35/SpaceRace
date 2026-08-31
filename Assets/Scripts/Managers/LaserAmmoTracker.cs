// Same idea as ScoreTracker - a small, plain class with no Unity
// dependencies, owning just the ammo NUMBER and the two rules for how it
// changes (spending one shot, granting a bonus amount). Whether or not
// there's a UI Text element to show it on screen is GameManager's concern,
// not this class's.
public class LaserAmmoTracker
{
    public int Count { get; private set; }

    public LaserAmmoTracker(int startingCount)
    {
        Count = startingCount;
    }

    /// <summary>
    /// Spends exactly one shot of ammo if any is available. Returns false
    /// (and clamps Count at 0, never letting it go negative) if there was
    /// nothing left to spend - callers that only care "did firing actually
    /// cost anything" can use this return value directly instead of
    /// checking Count themselves both before and after.
    /// </summary>
    public bool TrySpendOne()
    {
        if (Count <= 0)
        {
            Count = 0;
            return false;
        }

        Count--;
        return true;
    }

    /// <summary>
    /// Grants bonus ammo - the opposite of TrySpendOne. Takes an amount
    /// rather than always adding a fixed number, so different pickups can
    /// be worth different amounts just by passing a different value in,
    /// without ever needing to touch this class again.
    /// </summary>
    public void Add(int amount)
    {
        Count += amount;
    }
}
