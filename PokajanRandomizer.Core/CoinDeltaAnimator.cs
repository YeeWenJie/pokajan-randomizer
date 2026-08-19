namespace PokajanRandomizer;

public static class CoinDeltaAnimator
{
    public static (int Coins, int Remaining) At(CoinDelta delta, double t)
    {
        if (t >= 1)
        {
            return (delta.NewCoins, 0);
        }

        if (t <= 0)
        {
            return (delta.OldCoins, delta.Change);
        }

        var eased = 1 - Math.Pow(1 - t, 3);
        var moved = (int)Math.Round(delta.Change * eased);
        return (delta.OldCoins + moved, delta.Change - moved);
    }

    public static string FormatLine(string name, int coins, int originalChange, int remaining) =>
        $"{name}  {coins}  {FormatRemaining(originalChange, remaining)}";

    public static string FormatRemaining(int originalChange, int remaining)
    {
        if (originalChange >= 0)
        {
            return $"+{remaining}";
        }

        return $"-{Math.Abs(remaining)}";
    }
}
