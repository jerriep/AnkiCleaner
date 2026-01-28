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
});
return app.Run(args);
