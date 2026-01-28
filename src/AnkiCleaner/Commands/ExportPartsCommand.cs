using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using AnkiCleaner.Models;
using CsvHelper;
using Spectre.Console.Cli;

namespace AnkiCleaner.Commands;

internal class ExportPartsCommand : AsyncCommand<ExportPartsSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ExportPartsSettings settings,
        CancellationToken cancellationToken
    )
    {
        var deck = JsonSerializer.Deserialize<AnkiDeck>(
            await File.ReadAllTextAsync(settings.Source, cancellationToken)
        );

        var partsOfSpeech = deck
            .Notes.Select(n => n.Fields[4])
            .Distinct()
            .Select(s => new ExportedPartOfSpeech { CurrentValue = s, NewValue = null });

        using (var writer = new StreamWriter(settings.Destination))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            await csv.WriteRecordsAsync(partsOfSpeech, cancellationToken);
        }

        return 0;
    }
}

internal class ExportPartsSettings : CommandSettings
{
    [CommandArgument(0, "<source>")]
    [Description("The CrowdAnki JSON file to analyze")]
    public required string Source { get; set; }

    [CommandArgument(1, "<destination>")]
    [Description("The destination CSV file to create")]
    public required string Destination { get; set; }
}
