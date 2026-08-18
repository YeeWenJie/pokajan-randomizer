using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PokajanRandomizer;

internal static class AssetResolver
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

    public static string AssetsRoot =>
        Path.Combine(AppContext.BaseDirectory, "assets", "memberCards");

    public static ImageSource? TryLoad(MemberCard member)
    {
        if (!GenerationFolders.TryGetValue(member.Generation, out var folder))
        {
            return null;
        }

        var path = Path.Combine(AssetsRoot, folder, $"{member.Member}.png");
        if (!File.Exists(path))
        {
            return null;
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }
}
