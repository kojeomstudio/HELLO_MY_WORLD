# Minecraft Feature Categorization (Session 09 - 2026-01-22)

Updated for today’s pass before implementation. Features are grouped by Core, Content, and Util for both client and server with the intended execution order.

## Client
- **Core**
  1. World map control profile v5 with stitched hydrology signature (status: implemented)
  2. Preview generator alignment for caves/rivers/lakes (status: implemented)
- **Content**
  1. River + lake overlay smoothing tied to hydrology edges (status: in-progress)
  2. Cave moisture debug overlay for map previews (status: planned)
- **Util**
  1. Proto fingerprint + profile signature guard before preview builds (status: implemented)
  2. StreamingAssets config/profile reload with version/pipeline hash (status: implemented)

## Server
- **Core**
  1. Hydrology-aware terrain pipeline retuned for cave/river/lake coupling (status: implemented)
  2. World map control manager/profile v5 with generation signature refresh (status: implemented)
- **Content**
  1. Cave sealing against hydrology seams and river mouths (status: implemented)
  2. Lake-river outflow shaping with erosion-aware shelves (status: implemented)
- **Util**
  1. Protobuf registry/handler validation with prototype enforcement (status: implemented)
  2. Config/profile hash auditing for map-control requests (status: implemented)

## Sequencing Notes
- Refresh data files (JSON + markdown) first, then apply terrain algorithm updates, then update map-control architecture, and finish with protobuf validation/tests.
- Keep JSON configs (`config/world.json`, `Assets/StreamingAssets/world-config.json`, profile) as the single source for tuning values.
