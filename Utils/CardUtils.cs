using server.Models;

namespace server.Utils;

public static class CardUtils
{
    public static List<Card> GenerateDeck()
    {
        return (
            from suit in Enum.GetValues<CardSuit>() 
            from rank in Enum.GetValues<CardRank>() 
            select new Card(suit, rank)).ToList();
    }
}