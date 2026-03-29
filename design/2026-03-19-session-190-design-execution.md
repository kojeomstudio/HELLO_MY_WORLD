# Session 190 Design Execution (2026-03-19)

## Objective
Extend data-driven game data pipeline with additional datasets: monsters, npcs, and character_stats.

## Design Principles
- All runtime content data must be JSON
- Authoring templates are Markdown, converted via C# tool
- Reference minetest_project for entity definitions and character attributes

## Data Schema Design

### monsters.json
```json
{
  "monsters": {
    "zombie": {
      "id": 1,
      "name": "Zombie",
      "health": 20,
      "damage": 3,
      "speed": 0.23,
      "behavior": "hostile",
      "drops": [...]
    }
  }
}
```

### npcs.json
```json
{
  "npcs": {
    "villager": {
      "id": 1,
      "name": "Villager",
      "health": 20,
      "behavior": "neutral",
      "trades": [...]
    }
  }
}
```

### character_stats.json
```json
{
  "character_stats": {
    "base_health": 20,
    "base_hunger": 20,
    "base_speed": 0.1,
    "level_scaling": {...}
  }
}
```

## Minetest Reference
- `minetest_project/src/content_mapnode.h` - Node/block definitions
- `minetest_project/src/object_properties.h` - Entity properties
- `minetest_project/src/serverenvironment.cpp` - Entity lifecycle

## Implementation Steps
1. Create JSON schema files in `config/` or `Assets/StreamingAssets/`
2. Update GameDataTemplateExporter to process new datasets
3. Validate JSON structure with smoke test
4. Document schema in `docs/`

## Success Criteria
- All 5 datasets generated: items, recipes, monsters, npcs, character_stats
- Build passes without errors
- Selftest exits with code 0
