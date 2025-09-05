# World Generation (Server) - Enhanced

This document describes the enhanced server-side procedural generation used for Minecraft-like worlds.

## Enhanced Pipeline (per chunk)
1. **Base terrain heightmap and biomes** - Multi-octave noise with biome-specific height modifiers
2. **Stratification** - Biome-aware layer generation (bedrock/stone/dirt/grass/sand/snow)
3. **Ore generation** - Strategic placement based on depth and biome
4. **Cave systems** - Multiple cave generation methods:
   - 3D noise-based cave networks
   - Random-walk worm caves with variable radius
   - Large caverns for underground exploration
5. **Underground features** - Lakes, lava pools, aquifers
6. **Dungeons** - Various types:
   - Small cobblestone rooms with treasure
   - Large multi-room dungeons
   - Corridor systems with multiple chambers
7. **Surface structures** - Villages, ruins, monuments
8. **Vegetation** - Biome-specific trees, grass, flowers based on density maps
9. **Lighting calculation** - Sky light and block light propagation

## New Features

### Enhanced Cave Generation
- **3D Noise Caves**: Use Simplex noise for realistic cave networks
- **Worm Caves**: Improved random-walk algorithm with variable radius and branching
- **Large Caverns**: Rare but spectacular underground spaces
- **Cave Biomes**: Different cave types based on surrounding terrain

### Advanced World Features
- **Underground Lakes**: Water-filled caverns with stone shores
- **Lava Pools**: Dangerous underground hazards and resources
- **Dungeon Variety**: Multiple room layouts and treasure distributions
- **Structure Generation**: Villages, ruins, and other surface features

### Improved Block Management
- **Physics Simulation**: Sand and gravel fall naturally
- **Block Breaking System**: Tool-based breaking times and appropriate drops
- **Lighting System**: Proper light propagation and updates
- **Chunk Updates**: Efficient neighbor chunk updates

## Technical Improvements

### Performance Optimizations
- Deterministic generation using `(chunkX,chunkZ,seed)`-derived random seeds
- Chunk-local generation prevents cross-chunk dependencies
- Efficient lighting calculations with proper light propagation
- Optimized database persistence with async operations

### Enhanced Data Structures
- Extended block types with metadata support
- Biome-specific generation parameters
- Enhanced chunk data with lighting information
- Proper entity spawn management

## Configuration Options
- **World Settings**: Customizable seeds, structure density, cave frequency
- **Biome Parameters**: Per-biome height, vegetation, and structure settings
- **Difficulty Scaling**: Dungeon frequency and mob spawners based on game difficulty
- **Physics Settings**: Toggle for realistic physics simulation

## Future Extensibility
- **Custom Biomes**: Easy addition of new biome types with unique features
- **Modular Structures**: Template-based building generation
- **Advanced Physics**: Water flow, lava simulation, plant growth
- **Dynamic Weather**: Weather-based terrain modifications
- **Underground Civilizations**: Complex dungeon networks with lore

## Usage Examples

### Basic World Generation
```csharp
var worldManager = new WorldManager(database, worldId);
var settings = new WorldSettings {
    Seed = 12345,
    DungeonDensity = 0.12,
    EnablePhysics = true
};
var chunk = await worldManager.GetChunkAsync(chunkX, chunkZ);
```

### Custom Structure Placement
```csharp
// Villages generate with 2% probability per chunk
settings.VillageDensity = 0.02;
// Ruins generate with 5% probability per chunk  
settings.RuinsDensity = 0.05;
```

