# Repository Guidelines

## Project Structure & Module Organization
Unity client assets live in `Assets/`, with gameplay scripts under `Assets/MyAssets/Scripts/` (notably `GameWorld/`, `Network/`, `UI/`). Generated Protobuf DTOs reside in `Assets/Generated/Protobuf/`. The .NET server sits in `GameServer/` (entry point `Program.cs`, request handlers in `Handlers/`, session logic in `SessionManager.cs`). Shared serialization contracts are in `SharedProtocol/`, while `proto/` contains the `.proto` sources; update docs alongside changes in `docs/`.

## Build, Test, and Development Commands
Run the server build with `dotnet build SharedProtocol/SharedProtocol.csproj` followed by `dotnet build GameServer/GameServer.csproj`. Start the server locally via `dotnet run --project GameServer -- --server`. For an end-to-end smoke test (server + test client), use `dotnet run --project GameServer -- --selftest`. Refresh client Protobuf code with `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` before reopening Unity (`6000.0.23f1`).

## Coding Style & Naming Conventions
C# follows Allman braces, explicit access modifiers, and PascalCase for types, methods, and properties. Locals and parameters use camelCase; private fields may use `_camelCase`. Unity scripts respect existing tab indentation; server-side code uses four spaces. Proto files stay snake_case and messages use PascalCase within the `Game.*` or `EnhancedMinecraftProtocol` packages.

## Testing Guidelines
Server tests live in .NET projects; run them with `dotnet test`. Prefer deterministic cases that cover networking handlers, serialization, and world generation utilities. Unity tests belong in `Assets/Tests/EditMode` or `Assets/Tests/PlayMode`, executed via the Unity Test Runner (NUnit). Keep test names descriptive and aligned with the component under test.

## Commit & Pull Request Guidelines
Adopt conventional commits such as `feat(server): add room status command`. PRs should outline scope, link relevant issues, and include a test plan (commands and results). Attach logs, screenshots, or protocol traces when behavior or networking changes. Update `docs/` when altering protocol or architecture details.

## Security & Configuration Tips
Never commit secrets or local database dumps. Default configuration is `server-config.json`; document new keys in both `GameServer/ServerConfig.cs` and the appropriate README or doc entry. Review changes for inadvertent credential exposure before submission.
