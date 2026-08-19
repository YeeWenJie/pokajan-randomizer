namespace PokajanRandomizer.Maui;

internal static class HintSettings
{
    private const string Key = "InfoHintShown";

    public static bool InfoHintShown
    {
        get => Preferences.Default.Get(Key, false);
        set => Preferences.Default.Set(Key, value);
    }
}
