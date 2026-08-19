namespace PokajanRandomizer;

public static class ShuffleInfo
{
    public static string BuildBody(int? cardsToRemove)
    {
        var cardsToRemoveText = cardsToRemove is int count
            ? count.ToString()
            : "extra cards until the deck is 100 (the exact number appears here after New Game)";

        return
            "1. Take out the 4 gen cards that you got (each character has 9 cards: 3 pink, 3 blue, 3 orange).\n" +
            "Check who the bonus card is, shuffle that character's 9 cards first, and take one out — that card is the bonus card.\n" +
            "2. Shuffle the remaining cards.\n" +
            $"3. Then take out {cardsToRemoveText} cards so that it can be a 100 card deck.\n" +
            "4. Then deal 7 cards to each person.";
    }
}
