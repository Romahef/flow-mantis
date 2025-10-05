using System.Text;
using System.Text.Json;

namespace SqlSyncService.Serialization;

/// <summary>
/// Streams JSON output without full materialization for large datasets.
/// </summary>
public class JsonStreamWriter
{
    /// <summary>
    /// Writes a JSON object containing multiple named arrays to a stream.
    /// </summary>
    public static async Task WriteResponseAsync(
        Stream outputStream,
        Dictionary<string, IAsyncEnumerable<Dictionary<string, object?>>> namedArrays,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        await using var writer = new Utf8JsonWriter(outputStream, new JsonWriterOptions
        {
            Indented = false // Compact JSON for network efficiency
        });

        writer.WriteStartObject();

        // Write each named array
        foreach (var (arrayName, rows) in namedArrays)
        {
            writer.WritePropertyName(arrayName);
            writer.WriteStartArray();

            await foreach (var row in rows.WithCancellation(cancellationToken))
            {
                WriteJsonObject(writer, row);
            }

            writer.WriteEndArray();
        }

        // Write metadata if provided (e.g., pagination info)
        if (metadata != null)
        {
            foreach (var (key, value) in metadata)
            {
                writer.WritePropertyName(key);
                WriteJsonValue(writer, value);
            }
        }

        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Writes a JSON object (dictionary) to the writer.
    /// </summary>
    private static void WriteJsonObject(Utf8JsonWriter writer, Dictionary<string, object?> obj)
    {
        writer.WriteStartObject();

        foreach (var (key, value) in obj)
        {
            writer.WritePropertyName(key);
            WriteJsonValue(writer, value);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a JSON value with appropriate type handling.
    /// </summary>
    private static void WriteJsonValue(Utf8JsonWriter writer, object? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case string s:
                writer.WriteStringValue(s);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case decimal d:
                writer.WriteNumberValue(d);
                break;
            case double dbl:
                writer.WriteNumberValue(dbl);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case DateTime dt:
                writer.WriteStringValue(dt.ToString("o"));
                break;
            case Guid g:
                writer.WriteStringValue(g.ToString());
                break;
            case JsonElement je:
                je.WriteTo(writer);
                break;
            case Dictionary<string, object?> dict:
                WriteJsonObject(writer, dict);
                break;
            default:
                // Fallback: serialize as string
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
