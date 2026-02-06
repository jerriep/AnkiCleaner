using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using AnkiCleaner.AI;
using AnkiCleaner.Models;
using Anthropic.Exceptions;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace AnkiCleaner.Commands;

public class EnrichCommand(IAiClient aiClient, ILogger<EnrichCommand> logger)
    : AsyncCommand<EnrichSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        EnrichSettings settings,
        CancellationToken cancellationToken
    )
    {
        var deck = JsonSerializer.Deserialize<AnkiDeck>(
            await File.ReadAllTextAsync(settings.JsonSourceFile, cancellationToken)
        );

        var notesToEnrich = deck
            .Notes.GroupBy(n => n.ThaiWord)
            .Where(g => g.Any(n => !n.Tags.Contains(Tags.Enriched)))
            .ToDictionary(g => g.Key, g => g.ToList());
        logger.LogInformation("Enriching {Count} Thai words", notesToEnrich.Count);

        if (settings.OnlyUpdated)
        {
            logger.LogInformation("Clearing existing notes so only updated ones appear in output");
            deck.Notes.Clear();
            deck.Notes.AddRange(notesToEnrich.SelectMany(n => n.Value));
        }

        await AnsiConsole
            .Progress()
            .Columns(
                new SpinnerColumn(),
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new NumberCompletedColumn(),
                new ElapsedTimeColumn(),
                new RemainingTimeColumn()
            )
            .StartAsync(async ctx =>
            {
                var enrichmentTask = ctx.AddTask(
                    "Enriching Thai words",
                    maxValue: notesToEnrich.Count
                );

                foreach (var kvp in notesToEnrich)
                {
                    var thaiWord = kvp.Key;
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        enrichmentTask.Description = $"Enriching {Markup.Escape(thaiWord)}";

                        logger.LogInformation("Start enriching {ThaiWord}", thaiWord);

                        var partsOfSpeechResponse = await aiClient.GetPartsOfSpeech(thaiWord);
                        var existingPartsOfSpeech = kvp.Value.Select(n => n.PartOfSpeech).ToList();
                        var matchingPartsOfSpeech = partsOfSpeechResponse
                            .PartsOfSpeech.Intersect(existingPartsOfSpeech)
                            .ToList();

                        // First, we want to try and match up the parts of speech returned by the AI
                        // to existing notes inside our Anki deck
                        await EnrichMatchingNotes(
                            thaiWord,
                            kvp.Value.Where(n => matchingPartsOfSpeech.Contains(n.PartOfSpeech))
                        );

                        // Now, we need to create enrichments for the parts of speech where we did not have existing
                        // Anki notes. For this, we can use any of the remaining Anki notes we did not update above,
                        // or create new ones as needed.
                        var notesWithoutMatchingPartOfSpeech = kvp
                            .Value.Where(n => !matchingPartsOfSpeech.Contains(n.PartOfSpeech))
                            .ToList();
                        var outstandingPartsOfSpeech = partsOfSpeechResponse
                            .PartsOfSpeech.Except(matchingPartsOfSpeech)
                            .ToList();
                        if (outstandingPartsOfSpeech.Count > 0)
                        {
                            notesWithoutMatchingPartOfSpeech = await EnrichOutstandingPartsOfSpeech(
                                thaiWord,
                                outstandingPartsOfSpeech,
                                notesWithoutMatchingPartOfSpeech,
                                newNote => deck.Notes.Add(newNote)
                            );
                        }

                        // Finally, whichever notes remain, we must mark them as unused
                        foreach (var remainingNote in notesWithoutMatchingPartOfSpeech)
                        {
                            remainingNote.Tags = [.. remainingNote.Tags, Tags.Unused];
                        }

                        // Finally, save the JSON to make sure we do not lose progress on any exception
                        await SaveNotes(deck, settings.JsonDestinationFile);

                        logger.LogInformation("Finished enriching {ThaiWord}", thaiWord);
                    }
                    catch (AnthropicBadRequestException e)
                    {
                        logger.LogError(
                            e,
                            "Error received from Anthropic while enriching {ThaiWord}. You're probably out of credits",
                            thaiWord
                        );
                        break;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error while enriching {ThaiWord}", thaiWord);
                    }

                    enrichmentTask.Increment(1);
                }
            });

        return 0;
    }

    private async Task<List<AnkiDeckNote>> EnrichOutstandingPartsOfSpeech(
        string thaiWord,
        List<string> partsOfSpeech,
        List<AnkiDeckNote> notesAvailableForUse,
        Action<AnkiDeckNote> noteAdded
    )
    {
        AnkiDeckNote GetNextAvailableNote()
        {
            if (notesAvailableForUse.Count == 0)
            {
                var newNote = AnkiDeckNote.Empty();
                noteAdded(newNote);

                return newNote;
            }

            var noteToUse = notesAvailableForUse[0];
            notesAvailableForUse.RemoveAt(0);

            return noteToUse;
        }

        foreach (var partOfSpeech in partsOfSpeech)
        {
            var noteToEnrich = GetNextAvailableNote();

            await EnrichNote(noteToEnrich, thaiWord, partOfSpeech);
        }

        return notesAvailableForUse;
    }

    private async Task EnrichMatchingNotes(string thaiWord, IEnumerable<AnkiDeckNote> notesToEnrich)
    {
        foreach (var note in notesToEnrich)
        {
            var partOfSpeech = note.PartOfSpeech;

            await EnrichNote(note, thaiWord, partOfSpeech);
        }
    }

    private async Task EnrichNote(AnkiDeckNote note, string thaiWord, string partOfSpeech)
    {
        var enrichWordResponse = await aiClient.EnrichWord(thaiWord, partOfSpeech);

        note.ClearAllFieldValues();
        note.EnglishWord = enrichWordResponse.EnglishTranslation.AsBulletedList();
        note.ThaiWord = thaiWord;
        note.PhoneticWord = enrichWordResponse.Romanization;
        note.PartOfSpeech = partOfSpeech;
        note.ContextAndUsage = enrichWordResponse.ContextUsage;
        note.Notes = enrichWordResponse.Notes;
        note.ThaiSentence = enrichWordResponse.ExampleSentencesThai.AsBulletedList();
        note.EnglishSentence = enrichWordResponse.ExampleSentencesEnglish.AsBulletedList();
        note.NearSynonyms = enrichWordResponse.NearSynonyms;
        note.Classifier = enrichWordResponse.Classifier;

        if (!note.Tags.Contains(Tags.Enriched))
        {
            note.Tags = [.. note.Tags, Tags.Enriched];
        }
    }

    private async Task SaveNotes(AnkiDeck deck, string fileName)
    {
        var destinationDirectory = Path.GetDirectoryName(fileName);
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        await File.WriteAllTextAsync(
            fileName,
            JsonSerializer.Serialize(
                deck,
                new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true,
                }
            )
        );
    }
}

public class EnrichSettings : CommandSettings
{
    [CommandArgument(0, "<source json>")]
    [Description("The CrowdAnki JSON file to enrich")]
    public required string JsonSourceFile { get; set; }

    [CommandArgument(1, "<destination json>")]
    [Description("The output CrowdAnki JSON to write after enrichment")]
    public required string JsonDestinationFile { get; set; }

    [CommandOption("--only-updated")]
    [Description("Only writes the updated (and new) notes to the output file")]
    [DefaultValue(false)]
    public bool OnlyUpdated { get; set; }
}

public class NumberCompletedColumn : ProgressColumn
{
    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        return new Text($"{task.Value:N0}/{task.MaxValue:N0}");
    }
}
