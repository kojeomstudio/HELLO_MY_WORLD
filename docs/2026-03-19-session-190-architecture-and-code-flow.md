# Session 190 Architecture and Code Flow (2026-03-19)

## Overview
This session extends the data-driven game data pipeline with additional entity datasets.

## Architecture

### Game Data Pipeline
```
Markdown Templates (design/*.md)
         |
         v
GameDataTemplateExporter (Tools/)
         |
         v
JSON Data Files (Assets/StreamingAssets/, config/)
         |
         v
Runtime Loaders (Unity Client, GameServer)
```

### Data Flow
1. **Authoring Phase**: Game designers create templates in Markdown
2. **Export Phase**: C# tool processes templates and generates JSON
3. **Runtime Phase**: GameServer and Unity client load JSON at startup

### File Locations
| Dataset | Source | Output |
|---------|--------|--------|
| items | design/items-template.md | Assets/StreamingAssets/items.json |
| recipes | design/recipes-template.md | config/recipes.json |
| monsters | design/monsters-template.md | Assets/StreamingAssets/monsters.json |
| npcs | design/npcs-template.md | Assets/StreamingAssets/npcs.json |
| character_stats | design/character-stats-template.md | Assets/StreamingAssets/character_stats.json |

## Code Flow

### Server-Side Loading
```
GameServer.cs
    -> DataDrivenConfigManager.LoadAllGameData()
        -> LoadItems()
        -> LoadRecipes()
        -> LoadMonsters()
        -> LoadNpcs()
        -> LoadCharacterStats()
```

### Client-Side Loading
```
GameDataManager.cs
    -> LoadAllDataFiles()
        -> LoadStreamingAssets("items.json")
        -> LoadStreamingAssets("monsters.json")
        -> LoadStreamingAssets("npcs.json")
        -> LoadStreamingAssets("character_stats.json")
```

## Minetest Alignment
- Entity properties defined in `object_properties.h`
- Content definitions in `content_mapnode.h`
- Script API exposes entity data via Lua tables

## Validation Points
- JSON schema validation on load
- Required field presence checks
- Cross-reference integrity (e.g., recipe items must exist in items.json)
