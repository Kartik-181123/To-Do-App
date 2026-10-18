namespace TodoWidget.Helpers;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using LiteDB;

/// <summary>
/// Handles seamless JSON serialization and deserialization of LiteDB's ObjectId
/// so that tasks synced to/from cloud keep their exact unique identifiers across devices.
/// </summary>
public sealed class ObjectIdJsonConverter : JsonConverter<ObjectId>
{
    public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? hex = reader.GetString();
            if (!string.IsNullOrWhiteSpace(hex) && hex.Length == 24)
            {
                try
                {
                    return new ObjectId(hex);
                }
                catch
                {
                }
            }
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Support legacy format where ObjectId was serialized as an object with properties
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            if (root.TryGetProperty("Timestamp", out var ts) &&
                root.TryGetProperty("Machine", out var m) &&
                root.TryGetProperty("Pid", out var p) &&
                root.TryGetProperty("Increment", out var inc))
            {
                try
                {
                    return new ObjectId(ts.GetInt32(), m.GetInt32(), p.GetInt16(), inc.GetInt32());
                }
                catch
                {
                }
            }
        }

        return ObjectId.NewObjectId();
    }

    public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value != null ? value.ToString() : ObjectId.NewObjectId().ToString());
    }
}
