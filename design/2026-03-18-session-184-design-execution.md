# Session 184 Design Execution Guide (2026-03-18)

## Overview
This document outlines the design principles and execution patterns established for the Minecraft-like game clone project.

## Data-Driven Architecture

### Template-to-JSON Pipeline
1. **Template Source**: `design/templates/game-data-template.md`
   - Human-readable markdown format
   - Contains dataset headings with JSON code blocks
   - Easy to review and version control

2. **Export Tool**: `Tools/GameDataTemplateExporter`
   - .NET 8.0+ compatible
   - Parses markdown and extracts JSON datasets
   - Normalizes and validates JSON output

3. **Output Location**: `config/game-data/*.json`
   - Consumed by server at runtime
   - Validated during selftest

### Current Game Data Categories
| Dataset | Type | Description |
|---------|------|-------------|
| items | Array | Item definitions (tools, food, materials) |
| recipes | Array | Crafting recipes with ingredients |
| monsters | Array | Enemy definitions with stats |
| npcs | Array | Villager roles and dialogue |
| character_stats | Object | Base stats and growth curves |

## Design Principles

### 1. Configuration Over Code
- All tunable values should be in JSON configuration files
- Server reads config at startup
- Enables hot-tuning without recompilation

### 2. Template-First Data Entry
- Use markdown templates for human editing
- Automated tool converts to JSON
- Validates data integrity before runtime use

### 3. Session-Based Documentation
- Each work session creates:
  - `plans/YYYY-MM-DD-session-N-work-plan.md`
  - `docs/YYYY-MM-DD-session-N-*.md`
  - `design/YYYY-MM-DD-session-N-*.md` (if design changes)

## Validation Checklist
Before committing:
- [ ] Run template exporter
- [ ] Build SharedProtocol and GameServer
- [ ] Run selftest
- [ ] Verify game-data validation passes
- [ ] Update session work plan with completion status
