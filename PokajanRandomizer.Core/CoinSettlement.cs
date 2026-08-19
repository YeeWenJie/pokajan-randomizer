namespace PokajanRandomizer;

public static class CoinSettlement
{
    public static void ResetCoins(IEnumerable<SeatState> seats)
    {
        foreach (var seat in seats)
        {
            seat.Coins = SeatState.StartingCoins;
        }
    }

    public static IReadOnlyList<CoinDelta> ApplySelfPulled(
        IReadOnlyList<SeatState> seats,
        SeatState winner,
        PayoutResult payout)
    {
        var share = payout.Total / 3;
        return seats.Select(seat =>
        {
            var change = seat.Id == winner.Id ? payout.Total : -share;
            return ApplyDelta(seat, change);
        }).ToList();
    }

    public static IReadOnlyList<CoinDelta> ApplyDiscarded(
        IReadOnlyList<SeatState> seats,
        SeatState winner,
        SeatState payer,
        PayoutResult payout)
    {
        return seats.Select(seat =>
        {
            if (seat.Id == winner.Id)
            {
                return ApplyDelta(seat, payout.Total);
            }

            if (seat.Id == payer.Id)
            {
                return ApplyDelta(seat, -payout.Total);
            }

            return new CoinDelta(seat, seat.Coins, 0, seat.Coins);
        }).ToList();
    }

    private static CoinDelta ApplyDelta(SeatState seat, int change)
    {
        var oldCoins = seat.Coins;
        seat.Coins += change;
        return new CoinDelta(seat, oldCoins, change, seat.Coins);
    }
}
