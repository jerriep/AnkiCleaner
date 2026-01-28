using System.Text.Json.Serialization;

namespace AnkiCleaner.Models;

public class AnkiDeck
{
    [JsonPropertyName("notes")]
    public List<AnkiDeckNote> Notes { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalFields { get; set; }
}

public class AnkiDeckNote
{
    [JsonPropertyName("__type__")]
    public string Type { get; set; }

    [JsonPropertyName("guid")]
    public string? Id { get; set; }

    [JsonPropertyName("note_model_uuid")]
    public string? ModelId { get; set; }

    [JsonPropertyName("fields")]
    public string[] Fields { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalFields { get; set; }
}
