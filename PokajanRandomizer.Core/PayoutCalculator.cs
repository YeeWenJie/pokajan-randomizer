namespace PokajanRandomizer;

public static class PayoutCalculator
{
    private const int BonusPerCard = 90;

    public static PayoutResult? TryCalculate(
        IReadOnlyList<ClaimedCard> cards,
        MemberCard bonusMember,
        IReadOnlyList<GenerationRow> rows)
    {
        if (cards.Count < 3)
        {
            return null;
        }

        var sameColor = cards.All(card => card.Color == cards[0].Color);
        ComboKind? kind = null;

        if (IsTriple(cards))
        {
            kind = ComboKind.Triple;
        }
        else if (IsFullGen(cards, rows, out var genSize))
        {
            kind = genSize switch
            {
                3 => ComboKind.Gen3,
                4 => ComboKind.Gen4,
                5 => ComboKind.Gen5,
                _ => null
            };
        }

        if (kind is null)
        {
            return null;
        }

        var tableRate = TableRate(kind.Value, sameColor);
        var bonusCount = cards.Count(card => IsSameMember(card.Member, bonusMember));
        var bonusExtra = bonusCount * BonusPerCard;
        return new PayoutResult(kind.Value, sameColor, bonusCount, tableRate, bonusExtra, tableRate + bonusExtra);
    }

    public static bool IsSameMember(MemberCard left, MemberCard right) =>
        string.Equals(left.Member, right.Member, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.Generation, right.Generation, StringComparison.OrdinalIgnoreCase);

    private static bool IsTriple(IReadOnlyList<ClaimedCard> cards)
    {
        if (cards.Count != 3)
        {
            return false;
        }

        var first = cards[0].Member;
        return cards.All(card => IsSameMember(card.Member, first));
    }

    private static bool IsFullGen(
        IReadOnlyList<ClaimedCard> cards,
        IReadOnlyList<GenerationRow> rows,
        out int genSize)
    {
        genSize = 0;
        var generation = cards[0].Member.Generation;
        if (cards.Any(card => !string.Equals(card.Member.Generation, generation, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var row = rows.FirstOrDefault(item =>
            string.Equals(item.Generation, generation, StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return false;
        }

        genSize = row.Members.Count;
        if (cards.Count != genSize)
        {
            return false;
        }

        var roster = row.Members.Select(member => member.Member).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var picked = cards.Select(card => card.Member.Member).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return picked.Count == genSize && picked.SetEquals(roster);
    }

    private static int TableRate(ComboKind kind, bool sameColor) => (kind, sameColor) switch
    {
        (ComboKind.Triple, false) => 120,
        (ComboKind.Gen3, false) => 180,
        (ComboKind.Gen4, false) => 300,
        (ComboKind.Gen5, false) => 480,
        (ComboKind.Triple, true) => 840,
        (ComboKind.Gen3, true) => 480,
        (ComboKind.Gen4, true) => 840,
        (ComboKind.Gen5, true) => 1800,
        _ => 0
    };
}
