using Newtonsoft.Json;

namespace Apps.Drupal.Utils;

public class UnixTimestampConverter : JsonConverter<DateTime>
{
    public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var value = reader.Value;
        if (value is string s && long.TryParse(s, out long timestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        throw new JsonSerializationException($"Invalid timestamp value: {value}");
    }

    public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
    {
        var unixTimestamp = ((DateTimeOffset)value).ToUnixTimeSeconds();
        writer.WriteValue(unixTimestamp.ToString());
    }
}