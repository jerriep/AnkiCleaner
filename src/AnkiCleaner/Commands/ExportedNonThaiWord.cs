using CsvHelper.Configuration.Attributes;

namespace AnkiCleaner.Commands;

public class ExportedNonThaiWord
{
    [Name("id"), Optional]
    public string Id { get; set; }

    [Name("current"), Optional]
    public string Current { get; set; }

    [Name("new"), Optional]
    public string New { get; set; }
}
