using server.Models;
using server.Utils;

namespace server.Extensions;

public static class ListExtensions
{
    public static List<T> Shuffle<T> (this List<T> list, Random rng)
    {
        var copy = list.Copy();
        var n = copy.Count;

        while (n > 1) 
        {
            var k = rng.Next(n--);
            (copy[n], copy[k]) = (copy[k], copy[n]);
        }

        return copy;
    }

    public static List<T> Copy<T> (this List<T> list)
    {
        var array = new T[list.Count];
        list.CopyTo(array);
        return array.ToList();
    }

    public static List<T> Splice<T>(this List<T> list, int count)
    {
        var range = list.GetRange(0, count);
        list.RemoveRange(0, count);
        return range;
    }
    
    public static bool IsThirtyOne(this IEnumerable<Card> hand)
    {
        return hand.Sum(card => card.Value) == 31;
    }

    public static bool IsFire(this IEnumerable<Card> hand)
    {
        return hand.All(card => card.Rank == CardRank.Ass);
    }

    public static bool IsThirtyAndAHalf(this IEnumerable<Card> hand)
    {
        return Enum.GetValues<CardRank>().Any(rank => hand.All(card => card.Rank == rank));
    }

    public static double Value(this IEnumerable<Card> hand)
    {
        var enumerable = hand as Card[] ?? hand.ToArray();
        if (enumerable.IsThirtyAndAHalf()) return 30.5;
        return Enum.GetValues<CardSuit>()
            .Select(suit => enumerable.Sum(card => card.Suit == suit ? card.Value : 0))
            .Max();
    }

    public static async Task Broadcast(this List<Client> clients, WebSocketServerMessage message)
    {
        foreach (var client in clients)
        {
            await client.SendAsync(message);
        }
    }
    
    public static async Task Broadcast(this List<Player> players, WebSocketServerMessage message)
    {
        foreach (var player in players)
        {
            await player.SendAsync(message);
        }
    }
}