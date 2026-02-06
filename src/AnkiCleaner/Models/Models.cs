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

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalFields { get; set; }

    public static AnkiDeckNote Empty() =>
        new AnkiDeckNote
        {
            Type = "Note",
            ModelId = "50a3cc1c-f9b8-11f0-aa2d-841b77e4b195",
            Fields = Enumerable.Repeat(string.Empty, 12).ToArray(),
            Tags = [],
        };
}

public static class AnkiDeckExtensions
{
    extension(AnkiDeckNote note)
    {
        public string Classifier
        {
            get => note.Fields[11];
            set => note.Fields[11] = value;
        }

        public string ContextAndUsage
        {
            get => note.Fields[5];
            set => note.Fields[5] = value;
        }

        public string EnglishSentence
        {
            get => note.Fields[9];
            set => note.Fields[9] = value;
        }

        public string EnglishWord
        {
            get => note.Fields[0];
            set => note.Fields[0] = value;
        }

        public string NearSynonyms
        {
            get => note.Fields[10];
            set => note.Fields[10] = value;
        }

        public string Notes
        {
            get => note.Fields[6];
            set => note.Fields[6] = value;
        }

        public string PhoneticWord
        {
            get => note.Fields[2];
            set => note.Fields[2] = value;
        }

        public string PartOfSpeech
        {
            get => note.Fields[4];
            set => note.Fields[4] = value;
        }

        public string ThaiSentence
        {
            get => note.Fields[7];
            set => note.Fields[7] = value;
        }

        public string ThaiWord
        {
            get => note.Fields[1];
            set => note.Fields[1] = value;
        }

        public void ClearAllFieldValues()
        {
            note.Fields = Enumerable.Repeat(string.Empty, 12).ToArray();
        }
    }
}
