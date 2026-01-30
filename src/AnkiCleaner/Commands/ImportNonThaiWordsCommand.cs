using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AnkiCleaner.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AnkiCleaner.Commands;

public class ImportNonThaiWordsCommand : AsyncCommand<ImportNonThaiWordsSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ImportNonThaiWordsSettings settings,
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
            await foreach (var word in csv.GetRecordsAsync<ExportedNonThaiWord>(cancellationToken))
            {
                var ankiDeckNote = deck.Notes.FirstOrDefault(n =>
                    string.Equals(n.Id, word.Id, StringComparison.Ordinal)
                );
                if (ankiDeckNote == null)
                {
                    AnsiConsole.MarkupLine(
                        $"[red]Could not found {Markup.Escape(word.Id)} ({Markup.Escape(word.Current)})[/]"
                    );
                    continue;
                }

                ankiDeckNote.Fields[1] = word.New;
                AnsiConsole.MarkupLine(
                    $"[green]Updated {Markup.Escape(word.Id)} from {Markup.Escape(word.Current)} to {Markup.Escape(word.New)}[/]"
                );
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

public class ImportNonThaiWordsSettings : CommandSettings
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
