using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace AnkiCleaner.AI;

public class AnthropicAiClient : IAiClient
{
    private readonly AnthropicClient _anthropicClient;
    private readonly ResiliencePipeline _pipeline;

    public AnthropicAiClient(AnthropicClient anthropicClient, ILogger<AnthropicAiClient> logger)
    {
        _anthropicClient = anthropicClient;

        var options = new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<JsonException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Constant,
            OnRetry = args =>
            {
                logger.LogWarning(
                    "Retry {Attempt}: {Error}",
                    args.AttemptNumber + 1,
                    args.Outcome.Exception?.Message
                );

                return default; // Required for ValueTask
            },
        };

        _pipeline = new ResiliencePipelineBuilder().AddRetry(options).Build();
    }

    public async Task<PartsOfSpeechResponse?> GetPartsOfSpeech(string thaiWord)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            var message = await _anthropicClient.Messages.Create(
                new MessageCreateParams
                {
                    MaxTokens = 20000,
                    Temperature = 1,
                    System = """
                    You are a Thai language expert. Your task is to analyze Thai input (which may be a single word, phrase, or grammar pattern) and determine ALL grammatical functions or categories it represents.

                    Research thoroughly and identify every grammatical role or category. Use these Thai grammatical categories:

                    **For single words:**
                    - **stative verb** (กริยาบอกสภาพ) - verbs that describe states or conditions (e.g., สงบ meaning "to be calm")
                    - **action verb** (กริยาแสดงอาการ) - verbs that describe actions (e.g., วิ่ง meaning "to run")
                    - **noun** (คำนาม) - names of people, places, things, concepts
                    - **adjective** (คุณศัพท์) - true adjectives that modify nouns (rare in Thai)
                    - **adverb** (กริยาวิเศษณ์) - words that modify verbs or adjectives
                    - **preposition** (บุพบท) - words showing relationships
                    - **classifier** (ลักษณนาม) - words used for counting
                    - **particle** (อนุภาค) - grammatical particles
                    - **other** - specify if it's something else

                    **For multi-word units:**
                    - **phrase** - multi-word units that function together (e.g., อย่าทำอย่างนั้น)
                    - **expression** - idiomatic expressions or fixed sayings
                    - **grammar pattern** - structural templates with variables (e.g., ไม่ + adj + เท่าไร)

                    **IMPORTANT:**
                    - Most words that English speakers think of as "adjectives" are actually **stative verbs** in Thai
                    - If the input is a phrase, expression, or grammar pattern, identify it as such rather than trying to classify it as a single word
                    - Be precise about the Thai grammatical category

                    **OUTPUT FORMAT:**

                    Return your response as valid JSON ONLY. Do not include any preamble, explanatory text, or markdown code fences (no ```json).

                    Use this EXACT schema:

                    {
                      "thai_input": "string",
                      "parts_of_speech": ["string", "string"]
                    }

                    The "parts_of_speech" array must contain at least one grammatical category. List all grammatical roles or categories that apply using the categories above.
                    """,
                    Messages =
                    [
                        new()
                        {
                            Role = Role.User,
                            Content = $$"""
                            Analyze the Thai word: {{thaiWord}}

                            **CRITICAL OUTPUT REQUIREMENT:**

                            Your response must be PURE JSON with absolutely NO additional formatting:
                            - NEVER, EVER include anything other than the JSON response
                            - NO markdown code fences (no ```)
                            - NO ```json prefix
                            - NO explanatory text before or after
                            - ONLY the raw JSON object starting with { and ending with }

                            Your response should start immediately with the opening brace {
                            """,
                        },
                    ],
                    Model = Model.ClaudeSonnet4_5_20250929,
                }
            );

            var jsonResponse = message.Content.First().Json.GetProperty("text").GetString();
            return JsonSerializer.Deserialize<PartsOfSpeechResponse>(jsonResponse);
        });
    }

    public async Task<EnrichWordResponse> EnrichWord(string thaiWord, string partOfSpeech)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            var message = await _anthropicClient.Messages.Create(
                new MessageCreateParams
                {
                    MaxTokens = 20000,
                    Temperature = 1,
                    System = """
                    You are a Thai language expert. Your task is to create a detailed Anki flashcard for a Thai word or phrase when it functions in a SPECIFIC grammatical role.

                    **CRITICAL INSTRUCTIONS:**

                    - You will be given a Thai word or phrase AND a specific grammatical function (e.g., "stative verb", "action verb", "noun")
                    - Create ONLY ONE card for that word functioning ONLY in that specific grammatical role
                    - If the word has multiple meanings within that grammatical function, include ALL meanings on the SAME card
                    - Do NOT include information about how the word functions in other grammatical roles

                    **Understanding Thai Grammatical Categories:**

                    - **stative verb** - describes states/conditions, not actions (what English speakers often call adjectives)
                    - **action verb** - describes actions, events, or processes
                    - **noun** - names of entities, concepts
                    - Other categories as applicable

                    When creating the card, focus ONLY on how the word functions in the specified grammatical role.

                    **Card Structure:**

                    **Thai:** [the word in Thai script]

                    **Romanization:** [phonetic pronunciation with tone marks]

                    **English Translation**: [List each DISTINCT meaning within THIS SPECIFIC grammatical function as separate bullet points. If multiple English translations represent the SAME underlying meaning, group them together in a single bullet point separated by ' / '. Only create separate bullet points for genuinely different meanings.]
                    - [meaning 1 / alternate way to express meaning 1]
                    - [meaning 2]
                    - [meaning 3, if applicable]

                    **Context/Usage:** [Detailed explanation of when/how to use this word for EACH meaning within THIS grammatical function. Structure with clear labels :]
                    - <strong>Meaning 1 ([brief descriptor]):</strong> [Context and usage explanation]
                    - <strong>Meaning 2 ([brief descriptor]):</strong> [Context and usage explanation]
                    - <strong>Meaning 3 ([brief descriptor]):</strong> [Context and usage explanation, if applicable]

                    **Near-Synonyms/Contrasts:** [If applicable - list Thai near-synonyms for any of the meanings within this grammatical function and explicitly explain how this word differs from them in meaning, usage, context, scope, or formality. If no relevant near-synonyms, note "N/A"]

                    **Part of Speech:** [The specific grammatical function provided - must match exactly what was requested]

                    **Classifier:** [If applicable and the grammatical function is noun - the classifier used with this noun, otherwise note "N/A"]

                    **Example Sentences (Thai):** [2-3 bullet points PER MEANING showing the word used in context in THIS grammatical function, clearly labeled by meaning]
                    - [sentence 1 - Meaning 1]
                    - [sentence 2 - Meaning 1]
                    - [sentence 3 - Meaning 2]
                    - [sentence 4 - Meaning 2]
                    - [etc.]

                    **Example Sentences (English):** [Translations of the above, in the same order]
                    - [translation 1 - Meaning 1]
                    - [translation 2 - Meaning 1]
                    - [translation 3 - Meaning 2]
                    - [translation 4 - Meaning 2]
                    - [etc.]

                    **Notes:** [Any additional memory aids, common collocations, or formality markers SPECIFIC TO THIS GRAMMATICAL FUNCTION]

                    ---

                    **OUTPUT FORMAT:**

                    Return your response as valid JSON ONLY. Do not include any preamble, explanatory text, or markdown code fences (no ```json).

                    Use this EXACT schema:

                    {
                        "thai": "string",
                        "romanization": "string",
                        "english_translation": ["meaning 1", "meaning 2"],
                        "context_usage": "list with labelled meanings as described above. The items MUST be in the list format as described with each list item starting with a '-' and use <br> as a new line separator",
                        "near_synonyms": "list of synonyms using <br> as a new line separator, or N/A if not applicable",
                        "part_of_speech": "string (must match the provided grammatical function)",
                        "classifier": "string or N/A",
                        "example_sentences_thai": ["sentence 1", "sentence 2", "..."],
                        "example_sentences_english": ["translation 1", "translation 2", "..."],
                        "notes": "string"
                    }

                    All fields are required. Return exactly ONE card object that focuses ONLY on the specified grammatical function.
                    """,
                    Messages =
                    [
                        new()
                        {
                            Role = Role.User,
                            Content = $$"""
                            Create an Anki card for the Thai '{{thaiWord}}'  when it functions as a '{{partOfSpeech}}'. Focus ONLY on this grammatical function and do not include information about other grammatical roles.

                            **CRITICAL OUTPUT REQUIREMENT:**

                            Your response must be PURE JSON with absolutely NO additional formatting:
                            - NEVER, EVER include anything other than the JSON response
                            - NO markdown code fences (no ```)
                            - NO ```json prefix
                            - NO explanatory text before or after
                            - ONLY the raw JSON object starting with { and ending with }

                            Your response should start immediately with the opening brace {
                            """,
                        },
                    ],
                    Model = Model.ClaudeSonnet4_5_20250929,
                }
            );

            var jsonResponse = message.Content.First().Json.GetProperty("text").GetString();
            return JsonSerializer.Deserialize<EnrichWordResponse>(jsonResponse);
        });
    }
}
