namespace PokajanRandomizer;

public static class AssetPaths
{
    private static readonly Dictionary<string, string> GenerationFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Gen0"] = "Gen 0",
        ["Gen1"] = "Gen 1",
        ["Gen2"] = "Gen 2",
        ["Gen3"] = "Gen 3",
        ["Gen4"] = "Gen 4",
        ["Gen5"] = "Gen 5",
        ["ID Gen1"] = "ID Gen 1",
        ["ID Gen2"] = "ID Gen 2",
        ["ID Gen3"] = "ID Gen 3",
        ["Gamers"] = "Gamers",
        ["HoloX"] = "HoloX",
        ["Myth"] = "Myth",
        ["Promise"] = "Promise",
        ["Advent"] = "Advent",
        ["ReGloss"] = "ReGloss"
    };

    public static bool TryGetRelativePath(MemberCard member, out string relativePath)
    {
        if (member is null || !GenerationFolders.TryGetValue(member.Generation, out var folder))
        {
            relativePath = string.Empty;
            return false;
        }

        relativePath = Path.Combine(folder, $"{member.Member}.png");
        return true;
    }

    public static bool TryGetFilePath(string assetsRoot, MemberCard member, out string fullPath)
    {
        if (!TryGetRelativePath(member, out var relative))
        {
            fullPath = string.Empty;
            return false;
        }

        fullPath = Path.Combine(assetsRoot, relative);
        return true;
    }

    public static string ToPackagePath(string relativePath) =>
        relativePath.Replace('\\', '/');
}
