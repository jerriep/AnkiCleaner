using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AnkiCleaner.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Spectre.Console.Cli;

namespace AnkiCleaner.Commands;

public class ImportPartsCommand : AsyncCommand<ImportPartsSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ImportPartsSettings settings,
        CancellationToken cancellationToken
    )
    {
        var deck = JsonSerializer.Deserialize<AnkiDeck>(
            await File.ReadAllTextAsync(settings.JsonSourceFile, cancellationToken)
        );

        using (var reader = new StreamReader(settings.CsvSource, new UTF8Encoding()))
        using (
            var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { })
        )
        {
            await foreach (
                var partOfSpeech in csv.GetRecordsAsync<ExportedPartOfSpeech>(cancellationToken)
            )
            {
                if (string.IsNullOrEmpty(partOfSpeech.NewValue))
                {
                    // Leave part of speech as-is if there is no new value
                    continue;
                }

                foreach (
                    var ankiDeckNote in deck.Notes.Where(n =>
                        string.Equals(
                            n.PartOfSpeech,
                            partOfSpeech.CurrentValue,
                            StringComparison.Ordinal
                        )
                    )
                )
                {
                    ankiDeckNote.PartOfSpeech =
                        partOfSpeech.NewValue == "-" ? string.Empty : partOfSpeech.NewValue;
                }
            }
        }

        var destinationDirectory = Path.GetDirectoryName(settings.JsonDestinationFile);
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        if (File.Exists(settings.JsonDestinationFile))
        {
            File.Delete(settings.JsonDestinationFile);
        }

        await File.WriteAllTextAsync(
            settings.JsonDestinationFile,
            JsonSerializer.Serialize(
                deck,
                new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true,
                }
            ),
            cancellationToken
        );

        return 0;
    }
}

public class ImportPartsSettings : CommandSettings
{
    [CommandArgument(0, "<source csv>")]
    [Description("The CSV file containing the cleaned up parts of speech to import")]
    public required string CsvSource { get; set; }

    [CommandArgument(1, "<source json>")]
    [Description("The CrowdAnki JSON file to apply the changes to")]
    public required string JsonSourceFile { get; set; }

    [CommandArgument(2, "<destination json>")]
    [Description("The output CrowdAnki JSON to write after changes are applied")]
    public required string JsonDestinationFile { get; set; }
}
