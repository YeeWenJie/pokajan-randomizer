namespace PokajanRandomizer;

public static class GenerationLabels
{
    public static string For(string generation) => generation switch
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
        "ReGloss" => "Re",
        "ID Gen1" => "ID1",
        "ID Gen2" => "ID2",
        "ID Gen3" => "ID3",
        _ => generation
    };
}
