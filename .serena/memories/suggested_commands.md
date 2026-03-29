# Useful Commands
- Build shared protocol library: `dotnet build SharedProtocol/SharedProtocol.csproj`
- Build dedicated game server: `dotnet build GameServer/GameServer.csproj`
- Run full server self-test: `dotnet run --project GameServer -- --selftest`
- Launch server locally: `dotnet run --project GameServer -- --server`
- Regenerate protobuf C# contracts: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`
- Execute .NET solution tests: `dotnet test`
- Unity editor target version: open with Unity 6000.0.23f1