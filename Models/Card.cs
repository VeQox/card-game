using Newtonsoft.Json;

namespace server.Models;

public class Card
{
    [JsonProperty("suit")] public CardSuit Suit { get; }
    [JsonProperty("rank")] public CardRank Rank { get; }
    [JsonIgnore] public int Value { get; }

    public Card(CardSuit suit, CardRank rank)
        => (Suit, Rank, Value) = (suit, rank, CalculateValue(rank));

    private static int CalculateValue(CardRank rank) 
        => rank switch
    {
        CardRank.Ass => 11,
        CardRank.King => 10,
        CardRank.Ober => 10,
        CardRank.Unter => 10,
        CardRank.Ten => 10,
        CardRank.Nine => 9,
        CardRank.Eight => 8,
        CardRank.Seven => 7,
        _ => throw new Exception("Invalid card rank supplied")
    };
}

public enum CardSuit
{
    Hearts,
    Leaves,
    Bells,
    Acorns
}

public enum CardRank
{
    Seven,
    Eight,
    Nine,
    Ten,
    Unter,
    Ober,
    King,
    Ass
}

