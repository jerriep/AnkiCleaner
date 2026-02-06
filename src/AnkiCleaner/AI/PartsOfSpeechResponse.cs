using System.Text.Json.Serialization;

namespace AnkiCleaner.AI;

public record PartsOfSpeechResponse(
    [property: JsonPropertyName("thai_input")] string ThaiInput,
    [property: JsonPropertyName("parts_of_speech")] string[] PartsOfSpeech
);

public record EnrichWordResponse(
    [property: JsonPropertyName("thai")] string Thai,
    [property: JsonPropertyName("romanization")] string Romanization,
    [property: JsonPropertyName("english_translation")] string[] EnglishTranslation,
    [property: JsonPropertyName("context_usage")] string ContextUsage,
    [property: JsonPropertyName("near_synonyms")] string NearSynonyms,
    [property: JsonPropertyName("part_of_speech")] string PartOfSpeech,
    [property: JsonPropertyName("classifier")] string Classifier,
    [property: JsonPropertyName("example_sentences_thai")] string[] ExampleSentencesThai,
    [property: JsonPropertyName("example_sentences_english")] string[] ExampleSentencesEnglish,
    [property: JsonPropertyName("notes")] string Notes
);
