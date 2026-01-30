using AnkiCleaner.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.AddBranch(
        "parts",
        parts =>
        {
            parts.SetDescription("Commands to manage Parts of Speech");

            parts.AddCommand<ExportPartsCommand>("export");
            parts.AddCommand<ImportPartsCommand>("import");
        }
    );
    config.AddBranch(
        "thaiwords",
        thaiWords =>
        {
            thaiWords.SetDescription("Commands to manage and clean up Thai words");

            thaiWords.AddBranch(
                "nonthai",
                nonthaiWords =>
                {
                    nonthaiWords.AddCommand<CleanNonThaiWordsCommand>("clean");
                    nonthaiWords.AddCommand<ListNonThaiWordsCommand>("list");
                    nonthaiWords.AddCommand<ExportNonThaiWordsCommand>("export");
                    nonthaiWords.AddCommand<ImportNonThaiWordsCommand>("import");
                }
            );
        }
    );
});
return app.Run(args);
