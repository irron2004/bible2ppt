# Repository Guidelines

## Project Structure & Module Organization
- `Bible2PPT.sln` ties together the WinForms client and supporting services.
- `Bible2PPT/` hosts the Windows Forms UI, presenters, and resource files that drive the PPT generator.
- `Bible2PPT.Services.*` projects isolate domain logic: `BibleService` handles Bible data and EF Core migrations, `BuildService` produces slide decks, `TemplateService` parses template metadata, and `BibleIndexService` exposes search indexes.
- Unit tests live in the sibling `*.Tests/` directories (for example `Bible2PPT.Services.BibleService.Tests/`); mirror the namespace of the code under test.
- `Template/` stores sample PowerPoint templates; keep custom assets here with descriptive names.

## Build, Test, and Development Commands
- `dotnet restore Bible2PPT.sln` ensures all service packages and tools are available.
- `dotnet build Bible2PPT.sln -c Debug` validates the solution and produces binaries into each project's `bin/` directory.
- `dotnet run --project Bible2PPT/Bible2PPT.csproj` launches the WinForms client for manual verification.
- `dotnet test Bible2PPT.sln --collect:"XPlat Code Coverage"` executes all xUnit projects and emits coverage via the bundled `coverlet.collector` (results under `TestResults/`).

## Coding Style & Naming Conventions
- The root `.editorconfig` enforces 4-space indentation for C#, UTF-8 BOM encoding, and sorted `using` directives with `System.*` first; run `dotnet format` before submitting to apply these rules.
- Favor `PascalCase` for types and public members, `camelCase` for locals/parameters, and `s_fieldNames` or `_fieldNames` for private state to match existing patterns.
- Keep braces even for single-line statements and prefer `var` where the type is apparent, consistent with repository defaults.

## Testing Guidelines
- Write new tests with xUnit `[Fact]`/`[Theory]` attributes and descriptive method names (`MethodUnderTest_State_ExpectedBehavior`).
- Store fixtures alongside the tests under `Resources/` folders when needed and clean up temporary files in test teardown.
- Aim to cover new branches and database interactions with in-memory providers; verify EF Core migrations using the supplied `DesignTimeBibleDbFactory`.

## Commit & Pull Request Guidelines
- Follow the existing Conventional Commit shorthand (`feat:`, `fix:`, `refactor:`) and keep subject lines under 72 characters.
- Reference related issues in the body, list manual test coverage, and attach screenshots or PPT samples when UI or template changes are involved.
- Pull requests should summarize scope, highlight migrations or template updates, and note any configuration steps for reviewers.
