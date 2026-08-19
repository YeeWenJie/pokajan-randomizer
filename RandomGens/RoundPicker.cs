using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PokajanRandomizer;

internal static class RoundPicker
{
    private const int PickCount = 4;
    private const int CardsPerMember = 9;
    private const int DeckTarget = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static MemberData LoadData()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("members.json")
            ?? throw new InvalidOperationException("Embedded members.json was not found.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<MemberData>(json, JsonOptions)
            ?? throw new InvalidOperationException("members.json could not be parsed.");
    }

    public static RoundResult CreateRound(MemberData data)
    {
        var pickedGenerations = PickGenerations(data.Generations.Keys.ToList(), data.ExclusivePairs, PickCount);
        var rows = pickedGenerations
            .Select(generation => new GenerationRow(
                generation,
                BuildLabel(generation),
                data.Generations[generation].Select(member => new MemberCard(generation, member)).ToList()))
            .ToList();

        var allMembers = rows.SelectMany(row => row.Members).ToList();
        var bonus = allMembers[Random.Shared.Next(allMembers.Count)];
        var cardsToRemove = Math.Max(0, allMembers.Count * CardsPerMember - 1 - DeckTarget);

        return new RoundResult(rows, bonus, cardsToRemove);
    }

    private static List<string> PickGenerations(List<string> generationNames, List<List<string>> exclusivePairs, int count)
    {
        var remaining = new List<string>(generationNames);
        var picked = new List<string>();

        while (remaining.Count > 0 && picked.Count < count)
        {
            var choice = remaining[Random.Shared.Next(remaining.Count)];
            remaining.Remove(choice);
            picked.Add(choice);

            var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in exclusivePairs)
            {
                if (pair.Count < 2)
                {
                    continue;
                }

                if (string.Equals(choice, pair[0], StringComparison.OrdinalIgnoreCase))
                {
                    blocked.Add(pair[1]);
                    continue;
                }

                if (string.Equals(choice, pair[1], StringComparison.OrdinalIgnoreCase))
                {
                    blocked.Add(pair[0]);
                }
            }

            remaining.RemoveAll(name => blocked.Contains(name));
        }

        if (picked.Count < count)
        {
            throw new InvalidOperationException(
                $"Could only pick {picked.Count} generations after exclusive-pair filters.");
        }

        return picked;
    }

    private static string BuildLabel(string generation) => generation switch
    {
        "Gen0" => "0",
        "Gen1" => "1",
        "Gen2" => "2",
        "Gen3" => "3",
        "Gen4" => "4",
        "Gen5" => "5",
        "Gamers" => "Ga",
        "Promise" => "Pr",
        "Myth" => "My",
        "HoloX" => "X",
        "Advent" => "Ad",
        "ReGloss" => "Rg",
        "ID Gen1" => "ID1",
        "ID Gen2" => "ID2",
        "ID Gen3" => "ID3",
        _ => generation
    };
}

internal sealed record RoundResult(
    IReadOnlyList<GenerationRow> Rows,
    MemberCard BonusMember,
    int CardsToRemove);

internal sealed record GenerationRow(
    string Generation,
    string Label,
    IReadOnlyList<MemberCard> Members);

internal sealed record MemberCard(
    string Generation,
    string Member);

internal sealed class MemberData
{
    [JsonPropertyName("generations")]
    public Dictionary<string, List<string>> Generations { get; set; } = new();

    [JsonPropertyName("exclusive_pairs")]
    public List<List<string>> ExclusivePairs { get; set; } = new();
}
