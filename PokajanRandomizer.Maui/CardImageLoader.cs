namespace PokajanRandomizer.Maui;

internal static class CardImageLoader
{
    private static readonly Dictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource? TryLoad(MemberCard member)
    {
        if (!AssetPaths.TryGetRelativePath(member, out var relative))
        {
            return null;
        }

        var packagePath = "memberCards/" + AssetPaths.ToPackagePath(relative);
        if (Cache.TryGetValue(packagePath, out var cached))
        {
            return cached;
        }

        var bytes = TryReadPackageBytes(packagePath) ?? TryReadPackageBytes(packagePath.Replace('/', '\\'));
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        var source = ImageSource.FromStream(() => new MemoryStream(bytes));
        Cache[packagePath] = source;
        return source;
    }

    private static byte[]? TryReadPackageBytes(string packagePath)
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(packagePath).GetAwaiter().GetResult();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
