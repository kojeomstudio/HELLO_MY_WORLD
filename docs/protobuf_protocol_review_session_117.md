# Protobuf Protocol Review - Session 117 (2026-02-24)

## Review Focus
- Confirm generated Google Protobuf contracts are still bound to runtime registry.
- Confirm descriptor fingerprints are stable and not drifting from reference artifacts.
- Confirm dummy probe validates required packet coverage and profile guards.

## Improvements Applied
- Added `FailOnReferenceReportDrift` guard to `GameServer/Testing/DummyProtocolClient.cs`.
- Added reference report fingerprint comparison against:
  - `ProtoFingerprint.DescriptorFingerprint`
  - `ProtoFingerprint.ComputeFingerprint()`
- Added failure conditions when persisted reference report contains:
  - `MissingRegistrations`
  - `UnregisteredMessageTypes`
- Added JSON config key in probe settings:
  - `failOnReferenceReportDrift: true`

## Related Files
- `GameServer/Testing/DummyProtocolClient.cs`
- `config/protocol_dummy_client.json`
- `GameServer/config/protocol_dummy_client.json`
- `config/proto_reference_report.json`

## Expected Outcome
- Proto probe fails fast when generated packet references drift or stale reports remain.
- Server/client packet handling stays aligned with regenerated protobuf outputs.

## Validation Result (2026-02-24)
- `scripts/verify_protobuf.ps1`: `Generated protobufs are up to date relative to proto sources.`
- `dotnet run --project GameServer/GameServer.csproj -- --proto-probe`: PASS
  - Descriptor fingerprint: `4922CE79B7C3DB9E6F55FB02AD41358F9A682F502D05F9E6229783527F1FA1B4`
  - Computed fingerprint: `4922CE79B7C3DB9E6F55FB02AD41358F9A682F502D05F9E6229783527F1FA1B4`
  - Required packet round-trip validation: `14/14` (`RoundTrip=True`)
  - Reference report output: `config/proto_reference_report.json`
  - Probe report output: `reports/proto_probe_report.json`
- Remaining WARN-only gaps:
  - Optional packet bindings not registered: `MultiBlockChange`, `InventoryUpdate`, `ItemUse`, `ItemDrop`, `ItemPickup`, `EntityUpdate`, `EntityInteract`, `ContainerOpen`, `ContainerClose`, `ContainerUpdate`.
