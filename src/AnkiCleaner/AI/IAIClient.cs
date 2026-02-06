namespace AnkiCleaner.AI;

public interface IAiClient
{
    Task<PartsOfSpeechResponse> GetPartsOfSpeech(string thaiWord);

    Task<EnrichWordResponse> EnrichWord(string thaiWord, string partOfSpeech);
}
