# Minecraft Feature Execution Tracking

This document enumerates the Minecraft-style features required across the Unity client and .NET server and captures the task queue for sequential delivery. Update this file whenever scope or status changes so future iterations can resume quickly.

## Feature Catalogue
| ID | Feature | Server Status | Client Status | Notes |
|----|---------|---------------|---------------|-------|
| F-01 | Authentication & session heartbeat | Done | Done | Login handlers issue tokens; Unity login flow covers reconnect and heartbeat. |
| F-02 | Player movement & state sync | Done | Done | Tick-based corrections already in place. |
| F-03 | Chunk streaming & caching | Done | Done | Cache hints and duplicate suppression live on both sides. |
| F-04 | Block interaction broadcast | Done | Done | Broadcast with drop metadata via SessionManager helpers. |
| F-05 | Item drop visibility | Done | Done | Unity surfaces drop events for UI and pickups. |
| F-06 | Chunk residency tracking | Done | Done | Residency registry with TTL pruning runs server-side; client maintains loaded set. |
| F-07 | Chunk residency eviction | Done | Passive | Server enforces budgets; client unaffected. |
| F-08 | Client chunk unload signal | Done | Done | Bidirectional unload handshake trims residency instantly. |
| F-09 | Inventory snapshot persistence | Done | Done | Server snapshots and Unity diff consumer online. |
| F-10 | World time and weather broadcasts | Done | Done | Server systems stream ticks; Unity drives lighting, HUD, and weather events. |
| F-11 | Entity interpolation and culling | Planned | Planned | Needs velocity deltas, buffers, and view radius heuristics. |
| F-12 | Crafting and container persistence | Planned | Planned | Builds on inventory diff support; requires shared container sync. |
| F-13 | Server status HUD | Done | Done | Overlay refreshes metrics automatically every 15 seconds. |
| F-14 | Weather FX and ambient audio | In progress | In progress | Weather controller routes intensity to particles and audio; asset wiring remains. |
| F-15 | Combat feedback and damage numbers | Planned | Planned | Requires combat log events and client damage indicators. |

## Active Task Queue (Oct 2025)
1. [x] Task-10A - Hook `MinecraftGameClient` into `TimeUpdateMessage`, expose events, and cache the latest world and day ticks.
2. [x] Task-10B - Drive Unity skybox lighting and ambient settings via a `WorldTimeController` using the cached ticks.
3. [x] Task-10C - Surface `WeatherChangeMessage` through client events and a `WorldWeatherController` for FX toggles.
4. [x] Task-10D - Present formatted time and weather status in the HUD (`MinecraftGameManager`).
5. [ ] Task-11A - Prototype remote entity interpolation buffers and distance-based culling.
6. [ ] Task-10E - Author ambient presets and scene bindings for the new weather controller.

## Recently Completed
- Unity inventory snapshot diff consumer kept hotbar in sync with server reconnections.
- Server chunk residency eviction now enforces TTL and budgets without leaks.
- Time and weather broadcasts now update Unity lighting, HUD, and FX controllers.

