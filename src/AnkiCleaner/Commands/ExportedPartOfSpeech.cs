using CsvHelper.Configuration.Attributes;

namespace AnkiCleaner.Commands;

internal class ExportedPartOfSpeech
{
    [Name("current"), Optional]
    public string? CurrentValue { get; init; }

    [Name("new"), Optional]
    public string? NewValue { get; init; }
}
