using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using AnkiCleaner.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using DryRunItem = (string Original, string Cleaned);

namespace AnkiCleaner.Commands;

public class CleanNonThaiWordsCommand : AsyncCommand<CleanNonThaiWordsSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CleanNonThaiWordsSettings settings,
        CancellationToken cancellationToken
    )
    {
        var deck = JsonSerializer.Deserialize<AnkiDeck>(
            await File.ReadAllTextAsync(settings.JsonSourceFile, cancellationToken)
        );

        var cardsWithNonThaiWords = deck.Notes.Where(n =>
            RegexHelpers.NonThaiWordsRegex().IsMatch(n.Fields[1])
        );

        List<Func<string, string>> cleaners =
        [
            // Words with a classifier
            s =>
            {
                if (
                    RegexHelpers.ThaiWordWithClassifier().Match(s) is { Success: true } match
                    && !string.IsNullOrWhiteSpace(match.Groups["thaiword"].Value)
                )
                {
                    return match.Groups["thaiword"].Value;
                }

                return s;
            },
            // Words surrounded by div
            s =>
            {
                if (
                    RegexHelpers.ThaiWordSurroundedByDiv().Match(s) is { Success: true } match
                    && !string.IsNullOrWhiteSpace(match.Groups["thaiword"].Value)
                )
                {
                    return match.Groups["thaiword"].Value;
                }

                return s;
            },
            // Words surrounded by span
            s =>
            {
                if (
                    RegexHelpers.ThaiWordSurroundedBySpan().Match(s) is { Success: true } match
                    && !string.IsNullOrWhiteSpace(match.Groups["thaiword"].Value)
                )
                {
                    return match.Groups["thaiword"].Value;
                }

                return s;
            },
            // Finally, trim all whitespace
            s => s.Trim(),
        ];

        var dryRunList = new List<DryRunItem>();
        foreach (var card in cardsWithNonThaiWords)
        {
            var thaiWord = card.Fields[1];
            var cleanedWord = thaiWord;

            foreach (var cleaner in cleaners)
            {
                cleanedWord = cleaner(cleanedWord);
            }

            if (settings.DryRun)
            {
                dryRunList.Add(new DryRunItem(thaiWord, cleanedWord));
            }
            else
            {
                card.Fields[1] = cleanedWord;
            }
        }

        if (settings.DryRun)
        {
            var table = new Table();
            table.AddColumn("Original");
            table.AddColumn("Cleaned");

            foreach (var item in dryRunList)
            {
                table.AddRow(new Text(item.Original), new Text(item.Cleaned));
            }

            AnsiConsole.Write(table);
        }
        else
        {
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
        }
        return 0;
    }
}

public class CleanNonThaiWordsSettings : CommandSettings
{
    [CommandArgument(0, "<source>")]
    [Description("The CrowdAnki JSON file to analyze")]
    public required string JsonSourceFile { get; set; }

    [CommandArgument(1, "<destination>")]
    [Description("The output CrowdAnki JSON to write after changes are applied")]
    public required string JsonDestinationFile { get; set; }

    [CommandOption("--dry-run")]
    [DefaultValue(false)]
    public bool DryRun { get; set; } = false;
}
