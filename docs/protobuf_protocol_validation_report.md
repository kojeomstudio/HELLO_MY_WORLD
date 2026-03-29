# Protobuf Protocol Validation Report
**Date**: 2026-01-20  
**Status**: In progress (focus: handler coverage, library hygiene)

## Executive summary
- Generated DTOs under `Assets/Generated/Protobuf/` target **Google.Protobuf** and are linked via `SharedProtocol/SharedProtocol.csproj`.
- Legacy protobuf-net (`ProtoBuf`) models still exist in the `GameProtocol` namespace; keep them only where `[ProtoContract]` types are serialized and avoid mixing on Google.Protobuf-only paths.
- Handler coverage for EnhancedMinecraftProtocol messages needs a validation pass to guarantee bindings at server startup.

## Findings
1) **Redundant protobuf-net usings** – `GameServer/SessionManager.cs` and `GameServer/Systems/EntitySyncService.cs` import `ProtoBuf` despite serializing Google.Protobuf DTOs for live packets. Keeping only required libraries avoids ambiguity.  
2) **Handler registration visibility** – EnhancedMinecraftProtocol handlers rely on runtime registration; add a validation step so missing bindings surface during bootstrap instead of silently dropping packets.  
3) **Config/schema parity** – `proto/*.proto` → `Assets/Generated/Protobuf/*.cs` stay in sync via `protoc`. Both server and client consume the linked files; ensure regeneration after schema edits.

## Recommended actions (today)
- Remove unused `using ProtoBuf;` statements on Google.Protobuf-first code paths; keep protobuf-net only where `Serializer` is used.  
- Add a startup validation that checks EnhancedMinecraftProtocol handler bindings against registered message types.  
- Keep Google.Protobuf as the default for generated DTOs; regenerate if proto definitions change.

## Next checks
- Confirm entity spawn/despawn, experience orbs, enchanting, effects, achievements, and statistics messages are bound to handlers.  
- Re-run `dotnet build` for `SharedProtocol` and `GameServer` after cleanup to verify namespace/using alignment.  
- Update docs with results and include the validation in test notes.
