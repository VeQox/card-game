using Newtonsoft.Json;

namespace server.Utils;

public record Result<T>(T? Value, bool Error);

public static class JsonUtils
{
    public static string Serialize<T>(T obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }

    public static Result<T> Deserialize<T>(string json)
    {
        var error = false;
        var val = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
        {
            Error = (_, args) =>
            {
                error = true;
                args.ErrorContext.Handled = true;
            }
        });

        return new Result<T>(val, error);
    }
}