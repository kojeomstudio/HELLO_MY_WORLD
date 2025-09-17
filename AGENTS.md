# Repository Guidelines

This guide helps contributors work efficiently on HELLO_MY_WORLD (Unity client + .NET server). Keep changes small, documented, and aligned with the structure below.

## Project Structure & Module Organization
- `Assets/` – Unity content. Game code under `Assets/MyAssets/Scripts/` (e.g., `GameWorld/`, `Network/`, `UI/`). Protobuf DTOs in `Assets/Generated/Protobuf/`.
- `GameServer/` – .NET 6 server (`Program.cs`, `Handlers/`, `SessionManager.cs`, `ServerConfig.cs`).
- `SharedProtocol/` – Shared framing/types for server communication.
- `proto/` – `.proto` sources; see `docs/networking-protocol.md` for message IDs.
- `docs/` – Architecture, networking, and world-generation notes.

## Build, Test, and Development Commands
- Unity: open the repo with Unity `6000.0.23f1`.
- Server build: `dotnet build SharedProtocol/SharedProtocol.csproj && dotnet build GameServer/GameServer.csproj`
- Run server: `dotnet run --project GameServer -- --server`
- Self-test (spins server + test client): `dotnet run --project GameServer -- --selftest`
- Generate client Protobuf: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` (then refresh Unity)

## Coding Style & Naming Conventions
- C#: Allman braces; explicit access modifiers. PascalCase for types/methods/properties; camelCase for locals/parameters. Private backing fields may use `_camelCase`.
- Indentation: match surrounding file. Unity scripts commonly use tabs; server projects use 4 spaces.
- Proto: snake_case filenames (`game_world.proto`), PascalCase messages, packages under `Game.*` (or `EnhancedMinecraftProtocol` where applicable).

## Testing Guidelines
- Server: add tests in .NET test projects and run with `dotnet test`. For smoke checks use `--selftest`.
- Unity: place tests in `Assets/Tests/EditMode` or `Assets/Tests/PlayMode` and run via Unity Test Runner (NUnit).
- Target networking handlers, serialization, and world-gen utilities; prefer fast, deterministic tests.

## Commit & Pull Request Guidelines
- Commits: use conventional prefixes (`feat:`, `fix:`, `docs:`, `refactor:`, `chore:`). Example: `feat(server): add room status command`.
- PRs: clear description, scope, linked issues, test plan (commands/output), and screenshots/logs for visible or network changes. Update `docs/` when protocol or architecture changes.

## Security & Configuration Tips
- Do not commit secrets or local DB dumps. Default config is `server-config.json`; document new keys in `GameServer/ServerConfig.cs` and README/docs.
