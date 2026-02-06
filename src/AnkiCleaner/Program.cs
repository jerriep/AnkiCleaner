using AnkiCleaner;
using AnkiCleaner.AI;
using AnkiCleaner.Commands;
using AnkiCleaner.DependencyInjection;
using Anthropic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Serilog;
using Spectre.Console.Cli;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

try
{
    Log.Information("Starting the host...");

    var cancellationTokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true; // Prevent immediate process termination
        cancellationTokenSource.Cancel();
        Console.WriteLine("Cancellation requested...");
    };

    await Host.CreateDefaultBuilder(args)
        .UseSerilog(
            (context, configuration) =>
            {
                configuration
                // .MinimumLevel.Verbose()
                // .MinimumLevel.Information()
                .WriteTo.File("Logs/log.txt", rollOnFileSizeLimit: true);
            }
        )
        .ConfigureServices(
            static (ctx, services) =>
            {
                services.AddTransient<LoggingHandler>();
                services
                    .AddHttpClient("AnthropicClient")
                    .AddHttpMessageHandler<LoggingHandler>()
                    .AddStandardResilienceHandler(options =>
                    {
                        options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
                        {
                            Timeout = TimeSpan.FromMinutes(4),
                        };
                        options.AttemptTimeout = new HttpTimeoutStrategyOptions
                        {
                            Timeout = TimeSpan.FromMinutes(2),
                        };
                        options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
                    });
                services.AddTransient<AnthropicClient>(provider =>
                {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var httpClient = provider
                        .GetRequiredService<IHttpClientFactory>()
                        .CreateClient("AnthropicClient");

                    return new AnthropicClient
                    {
                        ApiKey = configuration["ANTHROPIC_API_KEY"],
                        HttpClient = httpClient,
                    };
                });
                services.AddTransient<IAiClient, AnthropicAiClient>();
            }
        )
        .BuildApp()
        .RunAsync(args, cancellationTokenSource.Token);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush(); // Ensure all logs are written before exit
}

internal static class HostBuilderExtensions
{
    public static CommandApp BuildApp(this IHostBuilder builder)
    {
        var registrar = new TypeRegistrar(builder);
        var app = new CommandApp(registrar);
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

                    thaiWords.AddCommand<EnrichCommand>("enrich");
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
        return app;
    }
}
