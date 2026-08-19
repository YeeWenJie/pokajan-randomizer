using System.Reflection;
using System.Text.Json;

namespace PokajanRandomizer;

public static class RoundPicker
{
    private const int PickCount = 4;
    private const int CardsPerMember = 9;
    private const int DeckTarget = 100;
    private const int MaxGensPerSize = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static MemberData LoadData()
    {
        var assembly = typeof(RoundPicker).Assembly;
        using var stream = assembly.GetManifestResourceStream("members.json")
            ?? throw new InvalidOperationException("Embedded members.json was not found.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<MemberData>(json, JsonOptions)
            ?? throw new InvalidOperationException("members.json could not be parsed.");
    }

    public static RoundResult CreateRound(MemberData data)
    {
        var displayOrder = data.Generations.Keys
            .Select((name, index) => (name, index))
            .ToDictionary(item => item.name, item => item.index, StringComparer.OrdinalIgnoreCase);
        var pickedGenerations = PickGenerations(data.Generations, data.ExclusivePairs, PickCount)
            .OrderBy(generation => displayOrder.GetValueOrDefault(generation, int.MaxValue))
            .ToList();
        var rows = pickedGenerations
            .Select(generation => new GenerationRow(
                generation,
                GenerationLabels.For(generation),
                data.Generations[generation].Select(member => new MemberCard(generation, member)).ToList()))
            .ToList();

        var allMembers = rows.SelectMany(row => row.Members).ToList();
        var bonus = allMembers[Random.Shared.Next(allMembers.Count)];
        var cardsToRemove = Math.Max(0, allMembers.Count * CardsPerMember - 1 - DeckTarget);

        return new RoundResult(rows, bonus, cardsToRemove);
    }

    private static List<string> PickGenerations(
        Dictionary<string, List<string>> generations,
        List<List<string>> exclusivePairs,
        int count)
    {
        var remaining = generations.Keys.ToList();
        var picked = new List<string>();
        var sizeCounts = new Dictionary<int, int>();

        while (remaining.Count > 0 && picked.Count < count)
        {
            var choice = remaining[Random.Shared.Next(remaining.Count)];
            remaining.Remove(choice);
            picked.Add(choice);

            var size = generations[choice].Count;
            sizeCounts[size] = sizeCounts.GetValueOrDefault(size) + 1;
            if (size != 4 && sizeCounts[size] >= MaxGensPerSize)
            {
                remaining.RemoveAll(name => generations[name].Count == size);
            }

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
                $"Could only pick {picked.Count} generations after exclusive-pair and size filters.");
        }

        return picked;
    }
}
