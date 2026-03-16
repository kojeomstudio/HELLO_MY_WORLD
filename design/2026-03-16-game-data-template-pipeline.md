# Game Data Template Pipeline (2026-03-16)

## 1. Objective
- Keep game content authoring human-readable in Markdown.
- Convert validated template blocks into runtime JSON through a C# CLI tool.
- Ensure server/client use the same JSON payloads for deterministic behavior.

## 2. Pipeline
1. Author dataset templates in Markdown (`design/templates/*.md`).
2. Run exporter tool (`Tools/GameDataTemplateExporter`) to emit JSON files.
3. Commit generated JSON to `config/game-data/` (and mirror if needed).
4. Load JSON from runtime config paths in server/client startup.

## 3. Template Format
- Dataset heading:
  - `## dataset: <name>`
- The next fenced code block must be `json` and contain valid JSON.
- Example:

```md
## dataset: monsters
```json
[
  { "id": "zombie", "health": 20, "attack": 3 }
]
```
```

## 4. Export Command
- Build:
  - `dotnet build Tools/GameDataTemplateExporter/GameDataTemplateExporter.csproj`
- Run:
  - `dotnet run --project Tools/GameDataTemplateExporter/GameDataTemplateExporter.csproj -- --input design/templates/game-data-template.md --output config/game-data`

## 5. Validation Rules
- Invalid JSON block fails export.
- Duplicate dataset names fail export.
- Output file names are sanitized and written as `<dataset>.json`.

## 6. Runtime Consumption Policy
- Game data categories:
  - items
  - recipes
  - monsters
  - npcs
  - character_stats
- Config/constant tuning remains JSON under existing config files.
- Do not hardcode tunable content in C# unless it is static engine logic.

