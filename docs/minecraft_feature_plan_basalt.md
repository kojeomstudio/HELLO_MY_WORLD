# Basalt Block Feature Plan

This document outlines the implementation plan for adding a new block type, Basalt, to the game.

## 1. Overview

-   **Block Name:** Basalt
-   **Block ID:** (A new ID will be assigned, e.g., 20)
-   **Generation:** Basalt will generate in deep cave systems, often near lava level.
-   **Properties:** A hard, dark-colored stone.

## 2. Server-Side Implementation (`GameServer`)

### 2.1. Block Type Definition
-   A new block definition for Basalt needs to be added. This will likely be in an enum or a static class holding block properties.
-   File to modify: `GameCommon/Blocks/BlockType.cs` (or a similar file).

### 2.2. World Generation
-   The world generation logic in `GameServer/World/WorldManager.cs` must be updated.
-   The new Basalt block will be integrated into the `GenerateCavesInternal` method or a new terrain generation pass. It should appear at lower elevations (e.g., y < 32).

### 2.3. Protocol
-   No changes to the Protobuf protocol (`.proto` files) are expected for simply adding a new block type, as the chunk data likely transmits block IDs. This should be verified.

## 3. Client-Side Implementation (Unity)

### 3.1. Block Registration
-   The client needs to be aware of the new Basalt block ID.
-   File to modify: `Assets/Scripts/Minecraft/World/BlockData.cs` (or a similar file).

### 3.2. Rendering
-   The `ChunkRenderer.cs` (or equivalent mesh generator) needs to be updated to handle the new block ID.
-   This involves assigning the correct UV coordinates for the Basalt texture in the texture atlas.
-   File to modify: `Assets/Scripts/Minecraft/World/ChunkRenderer.cs`.

### 3.3. Visuals (Assets)
-   A new texture for Basalt must be added to the block texture atlas.
-   A material that uses this texture atlas needs to be updated or created.
-   (Note: Asset creation is outside the scope of this automated task. The code will be prepared to use these assets.)

## 4. Implementation Sequence

1.  **Define Block ID:** Add the Basalt block type and ID on both server (`GameCommon`) and client.
2.  **Update World Generation:** Modify `WorldManager.cs` on the server to place Basalt blocks in the world.
3.  **Update Renderer:** Modify `ChunkRenderer.cs` on the client to render the new block.
4.  **Test:** Compile the server and run the client to verify that Basalt blocks generate and render correctly.
