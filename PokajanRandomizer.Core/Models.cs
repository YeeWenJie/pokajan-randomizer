using System.Text.Json.Serialization;

namespace PokajanRandomizer;

public enum CardColor
{
    Orange,
    Blue,
    Pink
}

public enum ComboKind
{
    Triple,
    Gen3,
    Gen4,
    Gen5
}

public sealed record RoundResult(
    IReadOnlyList<GenerationRow> Rows,
    MemberCard BonusMember,
    int CardsToRemove);

public sealed record GenerationRow(
    string Generation,
    string Label,
    IReadOnlyList<MemberCard> Members);

public sealed record MemberCard(
    string Generation,
    string Member);

public sealed record ClaimedCard(MemberCard Member, CardColor Color);

public sealed record PayoutResult(
    ComboKind Kind,
    bool SameColor,
    int BonusCardCount,
    int TableRate,
    int BonusExtra,
    int Total);

public sealed class SeatState
{
    public const int StartingCoins = 1000;

    public SeatState(int id, string defaultName)
    {
        Id = id;
        DefaultName = defaultName;
        Name = defaultName;
        Coins = StartingCoins;
    }

    public int Id { get; }
    public string DefaultName { get; }
    public string Name { get; set; }
    public int Coins { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? DefaultName : Name.Trim();
}

public sealed record CoinDelta(SeatState Seat, int OldCoins, int Change, int NewCoins);

public sealed class MemberData
{
    [JsonPropertyName("generations")]
    public Dictionary<string, List<string>> Generations { get; set; } = new();

    [JsonPropertyName("exclusive_pairs")]
    public List<List<string>> ExclusivePairs { get; set; } = new();
}

public sealed class SlotDraft
{
    public MemberCard? Member { get; set; }
    public CardColor? Color { get; set; }
}
