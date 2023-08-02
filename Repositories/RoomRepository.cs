using server.Models;

namespace server.Repositories;

public class RoomRepository
{
    private const int RoomIdLength = 4;
    private const string RoomIdCharacters = "abcdefghijklmnopqrstuvwxyz";
    private Dictionary<string, Room> Rooms { get; }
    
    public RoomRepository()
    {
        Rooms = new Dictionary<string, Room>();
        Task.Run(CleanupInactiveRooms);
    }
    
    public Room CreateRoom(string name, int capacity, bool isPublic) {
        var room = new Room(GenerateUniqueId(), name, capacity, isPublic);
        Rooms.Add(room.Id, room);
        return room;
    }

    public Room? GetRoom(string id) {
        return Rooms.FirstOrDefault(item => item.Key == id).Value;
    }
    
    public List<Room> GetRooms()
    {
        return Rooms.Values.ToList();
    }

    private string GenerateUniqueId() {
        var random = new Random();
        string id;
        do {
            id = "";
            for(var i = 0; i < RoomIdLength; i++)
                id += RoomIdCharacters[random.Next(0, RoomIdCharacters.Length)];
        } while(Rooms.ContainsKey(id));
        return id;
    }
    
    private void CleanupInactiveRooms()
    {
        do
        {
            foreach (var (key, room) in Rooms)
            {
                if (room.ConnectedClients != 0) continue;
                if (room.CreatedAt > DateTime.Now.AddMinutes(-1)) continue;
                Rooms.Remove(key);
            }

            Thread.Sleep(10000);
        } while (!Environment.HasShutdownStarted);
    }
}