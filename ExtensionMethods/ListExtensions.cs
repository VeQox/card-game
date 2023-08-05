using server.Models;

namespace server.ExtensionMethods;

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

    private static List<T> Copy<T> (this List<T> list)
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

    private static bool IsThirtyAndAHalf(this IEnumerable<Card> hand)
    {
        return Enum.GetValues<CardRank>().Any(rank => hand.All(card => card.Rank == rank));
    }

    public static double CalculateValue(this IEnumerable<Card> hand)
    {
        var enumerable = hand as Card[] ?? hand.ToArray();
        if (enumerable.IsThirtyAndAHalf()) return 30.5;
        return Enum.GetValues<CardSuit>()
            .Select(suit => enumerable.Sum(card => card.Suit == suit ? card.Value : 0))
            .Max();
    }

    public static async Task Broadcast(this IEnumerable<Client> clients, WebSocketServerMessage message)
    {
        await Task.WhenAll(clients.Select(client => client.SendAsync(message)));
    }
    
    public static async Task Broadcast(this IEnumerable<Player> players, WebSocketServerMessage message)
    {
        await Task.WhenAll(players.Select(player => player.SendAsync(message)));
    }
}