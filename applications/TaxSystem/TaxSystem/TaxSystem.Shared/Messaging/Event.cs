using System.Text.Json;

namespace TaxSystem.Shared.Messaging;

public sealed class Event
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Event()
    {
    }

    public Event(string topic, params object?[] arguments)
    {
        Topic = topic;
        Arguments = arguments;
    }

    public string Topic { get; set; } = string.Empty;

    public object?[] Arguments { get; set; } = [];

    public string Type => Topic;

    public T? GetArgument<T>(int index)
    {
        var json = JsonSerializer.Serialize(Arguments[index], JsonOptions);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public override string ToString()
    {
        if (Arguments.Length == 0)
        {
            return $"event({Topic})";
        }

        return $"event({Topic},[{string.Join(",", Arguments.Select(Stringify))}])";
    }

    private static string Stringify(object? value)
    {
        return value?.ToString() ?? "null";
    }
}
