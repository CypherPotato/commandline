# CommandLine — Project Guidelines

Lightweight command-line argument parser library for .NET (`CypherPotato.CommandLine`). Zero dependencies, no reflection.

## Build

```bash
dotnet build
dotnet pack          # NuGet package auto-generated on build
```

Target: .NET Standard 2.1. Nullable reference types enabled.

## Architecture

- `CommandLineParser` — sealed public class, the only public API entry point. Uses `yield return` for lazy evaluation in `GetValues()` and `GetRemainder()`.
- `CommandLineSplitFormat` — public enum selecting the parsing strategy (AutoDetect, Bash, Windows).
- `BashCommandLineSplitter`, `WindowsCommandLineSplitter`, `AutoDetectCommandLineParser` — internal static strategy classes for platform-specific splitting. Not exposed publicly.

## Conventions

- Keep the API surface minimal — no builders, no fluent API, no reflection.
- All public members must have XML documentation comments.
- Internal splitter classes use implicit `internal` access (no modifier).
- The term "verb" is used for command-line parameter names (e.g., `longVerb`, `shortVerb`).
- Warnings for malformed input go to `Console.Error`, not exceptions.

## Code Style

- C# with nullable reference types (`?` for optional returns).
- Prefer `yield return` for lazy sequences.
- Functional C# patterns: pattern matching, inline switches, records for DTOs.
- Prefer `string[0..10]` over `Substring()`. Prefer `string.IsNullOrWhiteSpace()`.
