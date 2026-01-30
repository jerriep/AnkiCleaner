using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using AnkiCleaner.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AnkiCleaner.Commands;

public partial class ListNonThaiWordsCommand : AsyncCommand<ListNonThaiWordsSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ListNonThaiWordsSettings settings,
        CancellationToken cancellationToken
    )
    {
        var deck = JsonSerializer.Deserialize<AnkiDeck>(
            await File.ReadAllTextAsync(settings.Source, cancellationToken)
        );

        var cardsWithNonThaiWords = deck.Notes.Where(n =>
            RegexHelpers.NonThaiWordsRegex().IsMatch(n.Fields[1])
        );

        var table = new Table();
        table.AddColumn("#");
        table.AddColumn("ID");
        table.AddColumn("Word");

        int index = 1;

        foreach (var card in cardsWithNonThaiWords)
        {
            // Console.WriteLine(card.Fields[1]);
            table.AddRow(new Text(index++.ToString()), new Text(card.Id), new Text(card.Fields[1]));
        }

        AnsiConsole.Write(table);

        return 0;
    }
}

public class ListNonThaiWordsSettings : CommandSettings
{
    [CommandArgument(0, "<source>")]
    [Description("The CrowdAnki JSON file to analyze")]
    public required string Source { get; set; }
}
