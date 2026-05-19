# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test class
dotnet test --filter "ClassName=TextUtilsTests"

# Run the interactive TUI (from solution root or Lesson-1/)
dotnet run --project Lesson-1

# Run a specific lesson directly (skips the menu)
dotnet run --project Lesson-1 -- 01_01_structured
dotnet run --project Lesson-1 -- 01_01_interaction
dotnet run --project Lesson-1 -- 01_01_grounding [notes/file.md] [--force] [--batch=N]
```

## Configuration

`Lesson-1/appsettings.json` is **gitignored and not in the repo**. Copy the template and fill in your key:

```bash
cp Lesson-1/appsettings.example.json Lesson-1/appsettings.json
```

Never commit `appsettings.json` or any file containing real API keys — the repo is public.

The file is read via `Microsoft.Extensions.Configuration` and copied to the output dir on build:

| Key | Used by |
|---|---|
| `AI_API_KEY` | All lessons |
| `RESPONSES_API_ENDPOINT` | All lessons (OpenAI Responses API format) |
| `AI_MODEL` | Interaction, Structured |
| `GROUND_MODEL` | Grounding pipeline; appends `:online` for web search |

## Architecture

### Lesson registration pattern

`Program.cs` holds a single array of `(Key, Label, Description, Func<string[], Task> Run)` tuples. Each entry points to a `static Run()` method on a class inside its subfolder. Adding a new lesson means adding one tuple here and one class with `internal static async Task Run(string[] args)`.

Folders follow the naming `Lesson_NN_MM_name/` inside whichever `Lesson-N` project they belong to.

### Grounding pipeline (`Lesson_01_01_grounding/`)

Four sequential stages, each with file-based caching keyed on SHA256 of the source content + model name:

```
markdown → ConceptExtractor → ConceptDeduper → ConceptSearcher → HtmlGrounder → grounded.html
              concepts.json     dedupe.json     search_results.json
```

- **`ApiClient`** — shared HTTP client with 3-retry exponential backoff (retries on 429/500/502/503). Wraps the OpenAI Responses API: `Chat(model, input, textFormat?, tools?, reasoning?)`. Use `ExtractText`, `ExtractJson<T>`, `ExtractWebSources` to parse responses.
- **`GroundingConfig`** — resolves all paths relative to `AppContext.BaseDirectory/Lesson_01_01_grounding/`. The search model is `GROUND_MODEL + ":online"` (OpenRouter web search suffix).
- **`JsonSchemas`** — strict JSON schema objects passed as `text.format` in API requests to enforce structured output.
- **`PromptBuilders`** — static methods returning `object[]` message arrays (system + user) ready to pass as `input` to `ApiClient.Chat`.
- **Caching** — each pipeline stage reads its JSON file, compares `sourceHash`/`conceptsHash`/`model`, skips if unchanged. Pass `--force` to bypass.

### TUI (`Program.cs`)

Arrow-key menu when `Console.IsInputRedirected == false`; numbered text menu otherwise. `FlushInputBuffer()` is called after each lesson returns to discard any stdin left over from the lesson's own `ReadLine` calls.

### Tests (`Lesson-1.Tests/`)

Cover `HashUtils`, `TextUtils`, `PromptBuilders`, and `JsonSchemas`. The main project exposes internals via `<InternalsVisibleTo Include="Lesson-1.Tests" />` in the csproj.

## VS Code

`launch.json` sets `"console": "integratedTerminal"` — required for the arrow-key TUI. Running via the internal debug console falls back to the numbered text menu automatically.
