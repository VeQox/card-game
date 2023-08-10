using Newtonsoft.Json;

namespace server.Models;

public class Player : Client
{
    [JsonIgnore] public List<Card> Hand { get; set; }
    [JsonProperty("lives")] public int Lives { get; set; }
    [JsonIgnore] public bool HasSkipped { get; set; }
    [JsonIgnore] public bool HasLocked { get; set; }
    [JsonIgnore] public int TurnCount { get; set; }

    public Player(Client client) : base(client)
        => (Hand, Lives) = (new List<Card>(), 3);
}