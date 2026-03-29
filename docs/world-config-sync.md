# World Config Sync (Server <-> Unity)

The server world generation tuning lives in `config/world.json`, while Unity consumes a flattened JSON file at `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`.

To keep shared tuning keys (hydrology, caves, rivers, lakes, render/simulation distance) aligned, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync_world_config.ps1
```

To also overwrite Unity `ChunkSize` from the server config:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync_world_config.ps1 -UpdateChunkSize
```

Notes:
- The sync script only updates keys that exist in the server config and leaves Unity-only sizing fields (subworld sizes, tile unit, etc.) untouched unless you opt into `-UpdateChunkSize`.
- After syncing, reopen Unity (or reload the resource) so `WorldConfigFile` picks up the updated values.
- Hydrology variance keys (`HydrologyVarianceBlend`, `HydrologyVarianceClamp`) live in both configs; rerun the sync script when tuning them so MapGeneratorLib previews stay aligned with server streaming.
