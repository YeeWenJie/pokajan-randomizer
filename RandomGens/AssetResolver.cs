using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PokajanRandomizer;

internal static class AssetResolver
{
    public static string AssetsRoot =>
        Path.Combine(AppContext.BaseDirectory, "assets", "memberCards");

    public static ImageSource? TryLoad(MemberCard member)
    {
        if (!AssetPaths.TryGetFilePath(AssetsRoot, member, out var path) || !File.Exists(path))
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
