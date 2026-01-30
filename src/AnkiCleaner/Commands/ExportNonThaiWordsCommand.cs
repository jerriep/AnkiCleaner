using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using AnkiCleaner.Models;
using CsvHelper;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AnkiCleaner.Commands;

public class ExportNonThaiWordsCommand : AsyncCommand<ExportNonThaiWordsSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ExportNonThaiWordsSettings settings,
        CancellationToken cancellationToken
    )
    {
        var deck = JsonSerializer.Deserialize<AnkiDeck>(
            await File.ReadAllTextAsync(settings.Source, cancellationToken)
        );

        var cardsWithNonThaiWords = deck
            .Notes.Where(n => RegexHelpers.NonThaiWordsRegex().IsMatch(n.Fields[1]))
            .Select(c => new ExportedNonThaiWord
            {
                Id = c.Id,
                Current = c.Fields[1],
                New = c.Fields[1],
            });

        using (var writer = new StreamWriter(settings.Destination))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            await csv.WriteRecordsAsync(cardsWithNonThaiWords, cancellationToken);
        }

        return 0;
    }
}

public class ExportNonThaiWordsSettings : CommandSettings
{
    [CommandArgument(0, "<source>")]
    [Description("The CrowdAnki JSON file to analyze")]
    public required string Source { get; set; }

    [CommandArgument(1, "<destination>")]
    [Description("The destination CSV file to create")]
    public required string Destination { get; set; }
}
