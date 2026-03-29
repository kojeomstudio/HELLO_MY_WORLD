# HELLO_MY_WORLD project summary
- Open-source Unity voxel game mimicking Minecraft core mechanics with Unity client (`Assets/`) and .NET 6 server (`GameServer/`).
- Shared protobuf protocol in `SharedProtocol/`; `.proto` sources in `proto/` generate to `Assets/Generated/Protobuf/` using `protoc`.
- Additional modules: procedural generator library `MapGeneratorLib/`, legacy `KojeomNetWorkSpace/`, editor utilities `CustomToolSet/`, worldgen docs in `docs/`.
- Server entry at `GameServer/Program.cs`, handlers under `GameServer/Handlers/`, session management via `SessionManager.cs`. Client scripts organized in `Assets/MyAssets/Scripts/` (GameWorld, Network, UI, etc.).
- Project targets Windows, uses Unity 6000.0.23f1, MIT license. Main config `server-config.json` governs world/time/weather systems.