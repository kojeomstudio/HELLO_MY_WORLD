# Task Completion Checklist
- Build shared protocol and server projects: `dotnet build SharedProtocol/SharedProtocol.csproj` and `dotnet build GameServer/GameServer.csproj`.
- Run relevant tests, typically `dotnet test` or server self-test `dotnet run --project GameServer -- --selftest`.
- Update documentation in `docs/` or `README.md` when modifying protocols, features, or configuration.
- Ensure protobuf changes are regenerated via `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` before committing.
- Review git status, stage intended changes, commit with conventional commit message, and push to remote per repo guidelines.
- Double-check no secrets or generated artifacts are accidentally included in commits.