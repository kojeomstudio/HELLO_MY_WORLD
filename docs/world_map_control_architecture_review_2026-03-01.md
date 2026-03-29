# World Map Control Architecture Review
**Date**: 2026-03-01  
**Session**: 137  
**Status**: In Progress

## Overview

This document reviews the world map control architecture for server and client, identifying strengths, weaknesses, and improvement opportunities.

## Files Reviewed

1. **WorldMapControlManager.cs** (1,232 lines)
   - Lightweight world map control service
   - Manages profile tracking, chunk caching, queue management
   - Supports inflight chunk generation tracking
   - Complex adaptive queue policy with pressure bands

2. **WorldMapController.cs** (809 lines)
   - Centralized world map controller
   - Manages chunk generation and caching
   - Has queue pressure management
   - Cleanup timer for old chunks

## Strengths

### 1. Advanced Queue Management
- Adaptive queue limits based on system load
- Multiple pressure bands (Critical, High, Elevated, Normal, Low)
- Emergency brake mechanism for overload protection
- Backoff and recovery ramp for smooth transitions

### 2. Profile Management
- Per-player map preferences (render distance, map scale, quality settings)
- Profile versioning and hash-based change detection
- Automatic profile reloading on config changes

### 3. Caching Strategy
- Chunk cache with access time tracking
- Inflight chunk generation tracking with timeout
- Automatic cache pruning based on budget

### 4. Data-Driven Configuration
- Extensive use of config classes
- JSON-based configuration support
- Tunable parameters for all aspects of control

### 5. Signature-Based Validation
- Generation signature for cache invalidation
- Protocol fingerprint validation
- Config hash-based reload detection

## Critical Issues

### Issue 1. Duplicate Functionality (CRITICAL)

**Problem**: Two separate classes (`WorldMapControlManager` and `WorldMapController`) implement very similar functionality with different approaches.

**Evidence**:
- Both manage chunk generation and caching
- Both have queue pressure management
- Both have cleanup timers
- Both use similar config and profile structures

**Impact**: 
- Code duplication
- Maintenance burden
- Potential inconsistency between implementations
- Confusion about which to use

**Recommendation**: Merge into single unified controller with clear separation of concerns.

### Issue 2. Inconsistent Queue Policy Implementation

**Problem**: Queue policy logic is duplicated with slight variations between the two classes.

**Examples**:
- `WorldMapControlManager.GetAdaptiveQueueLimit()` (lines 625-793)
- `WorldMapController.GetAdaptiveQueueState()` (lines 521-585)

**Impact**: Different behavior depending on which controller is used, potential bugs.

**Recommendation**: Extract queue policy to shared utility class.

### Issue 3. Magic Numbers and Thresholds

**Problem**: Hard-coded constants scattered throughout both files.

**Examples**:
- `0.55`, `0.35`, `0.25` - blending weights
- `0.88`, `0.92`, `0.98` - load shedding thresholds
- `1.15`, `1.28`, `3.0` - slack ratios
- `0.42`, `0.24`, `0.22` - various weights

**Recommendation**: Define named constants in config classes.

### Issue 4. Complex Queue State Management

**Problem**: Queue state is computed on-demand with complex logic that's hard to reason about.

**Examples**:
- `GetAdaptiveQueueState()` computes 7 different values
- Emergency latch with multiple conditions
- Recovery ramp with countdowns

**Impact**: Difficult to debug and maintain, potential edge cases.

**Recommendation**: Simplify queue state machine with clear transitions.

### Issue 5. No Client-Side Implementation

**Problem**: Only server-side implementation exists. Client-side world map control is missing.

**Impact**: Client cannot manage map preferences or cache chunks locally.

**Recommendation**: Implement client-side counterpart.

## Code Organization Issues

### Issue 6. Long Methods

**Problem**: Some methods exceed 100 lines with complex nested logic.

**Examples**:
- `WorldMapControlManager.GetAdaptiveQueueLimit()` (169 lines)
- `WorldMapControlManager.ComputeGenerationSignature()` (116 lines)
- `WorldMapController.GetAdaptiveQueueState()` (65 lines)

**Recommendation**: Extract helper methods for:
- Queue policy computation
- Signature computation
- State transitions

### Issue 7. Repeated Patterns

**Problem**: Similar calculation patterns repeated across both classes.

**Examples**:
- File hash computation
- Queue pressure classification
- Cache budget computation
- Access time tracking

**Recommendation**: Create shared utility methods.

### Issue 8. Inconsistent Naming

**Problem**: Different naming conventions for similar concepts.

**Examples**:
- `inflightChunkGenerations` vs `generationTasks`
- `chunkAccessTimes` vs `accessTimes`
- `maxCachedChunks` vs `maxLoadedChunks`

**Recommendation**: Standardize naming across both classes.

## Performance Issues

### Issue 9. Unnecessary Dictionary Lookups

**Problem**: Some dictionary lookups could be cached or avoided.

**Location**: `WorldMapControlManager.TryGetNearestPlayerChunkFocus()` (lines 545-566)

**Impact**: O(n) lookup for each chunk in hot path.

**Recommendation**: Precompute or use spatial indexing.

### Issue 10. Repeated File System Access

**Problem**: File write times are checked multiple times per request.

**Location**: `MaybeReloadGenerationConfig()` and `EnsureProfile()` both check file times

**Impact**: Unnecessary I/O operations.

**Recommendation**: Cache file metadata with change notifications.

### Issue 11. Inefficient Pruning

**Problem**: Pruning iterates over entire cache multiple times.

**Location**: `EnforceCacheBudget()` (lines 885-926) and `EnforceLoadedChunkBudget()` (lines 614-656)

**Impact**: O(n) operations where O(k) would suffice.

**Recommendation**: Use priority queue or indexed access.

## Consistency Issues

### Issue 12. Different Clamping Thresholds

**Problem**: Different clamping thresholds for similar operations.

**Examples**:
- `Math.Clamp(value, 0.0, 1.35)` in manager
- `Math.Clamp(value, 0.0, 1.0)` in controller
- `Math.Clamp(value, 0.0, 1.2)` in controller

**Recommendation**: Standardize clamping ranges.

### Issue 13. Different Hash Computation Methods

**Problem**: Two different implementations of file hash computation.

**Examples**:
- `WorldMapControlManager.ComputeFileHash()` uses SHA256
- `WorldMapController.ComputeFileHash()` uses SHA256 but different format

**Recommendation**: Use single shared implementation.

### Issue 14. Inconsistent Logging

**Problem**: Different logging patterns and levels.

**Examples**:
- Manager uses `[WorldMapControlManager]` prefix
- Controller uses `[WorldMapController]` prefix
- Different log levels for similar operations

**Recommendation**: Standardize logging format and levels.

## Architecture Issues

### Issue 15. No Clear Separation of Concerns

**Problem**: Both classes mix responsibilities:
- Queue management
- Cache management
- Profile management
- Generation coordination
- Configuration management

**Impact**: Difficult to test and maintain, unclear ownership.

**Recommendation**: Separate into focused components:
- `WorldMapQueueManager`
- `WorldMapCacheManager`
- `WorldMapProfileManager`
- `WorldMapGenerationCoordinator`

### Issue 16. No Interface Abstraction

**Problem**: No common interface for world map control.

**Impact**: Tight coupling, difficult to mock for testing.

**Recommendation**: Define `IWorldMapController` interface.

### Issue 17. No Dependency Injection

**Problem**: Dependencies are instantiated directly in constructors.

**Impact**: Difficult to test, tight coupling.

**Recommendation**: Use dependency injection.

### Issue 18. Synchronous Operations

**Problem**: Some operations are synchronous when they could be async.

**Examples**:
- File I/O operations
- Hash computations

**Impact**: Blocks threads, poor scalability.

**Recommendation**: Make all I/O async.

## Missing Features

### Issue 19. No Chunk Prioritization by Player Movement

**Problem**: No prediction of player movement for proactive chunk loading.

**Impact**: Reactive rather than proactive loading, potential lag.

**Recommendation**: Implement movement prediction and preloading.

### Issue 20. No Chunk Quality Levels

**Problem**: All chunks have same quality regardless of importance.

**Impact**: Wasted resources on distant chunks.

**Recommendation**: Implement quality levels (LOD system).

### Issue 21. No Chunk Streaming

**Problem**: Chunks are loaded all at once, not streamed.

**Impact**: Long load times for large areas.

**Recommendation**: Implement progressive streaming.

### Issue 22. No Client-Side Caching

**Problem**: No client-side chunk cache or prediction.

**Impact**: Network bandwidth waste, slower loading.

**Recommendation**: Implement client-side caching and prediction.

## Priority Recommendations

### Critical Priority
1. **Merge duplicate controllers** - Eliminates code duplication
2. **Extract queue policy to shared class** - Prevents inconsistencies
3. **Define named constants** - Improves maintainability

### High Priority
4. **Separate concerns into focused components** - Improves architecture
5. **Implement interface abstraction** - Improves testability
6. **Make I/O operations async** - Improves scalability

### Medium Priority
7. **Simplify queue state machine** - Improves debuggability
8. **Implement client-side controller** - Enables client caching
9. **Add movement prediction** - Improves loading
10. **Implement quality levels** - Optimizes resources

### Low Priority
11. **Standardize naming** - Improves consistency
12. **Optimize dictionary lookups** - Performance
13. **Cache file metadata** - Performance
14. **Implement chunk streaming** - Feature enhancement

## Implementation Plan

### Phase 1: Critical Fixes
- [ ] Merge `WorldMapControlManager` and `WorldMapController` into unified controller
- [ ] Extract queue policy to `WorldMapQueuePolicy` class
- [ ] Define all magic numbers as named constants
- [ ] Create `IWorldMapController` interface

### Phase 2: Architecture Improvements
- [ ] Separate into focused components
- [ ] Implement dependency injection
- [ ] Make all I/O async
- [ ] Standardize naming conventions

### Phase 3: Performance Optimizations
- [ ] Cache file metadata
- [ ] Optimize dictionary lookups
- [ ] Improve pruning efficiency
- [ ] Precompute terrain metrics

### Phase 4: Feature Additions
- [ ] Implement client-side controller
- [ ] Add movement prediction
- [ ] Implement quality levels
- [ ] Add chunk streaming

## Testing Strategy

### Unit Tests
- Test queue policy computation
- Test cache budget enforcement
- Test profile management
- Test signature computation

### Integration Tests
- Test controller coordination
- Verify cross-chunk continuity
- Test queue pressure handling

### Performance Tests
- Measure generation time per chunk
- Profile hotspots
- Compare before/after optimization

## Proposed Architecture

### Unified WorldMapController

```csharp
public interface IWorldMapController
{
    Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ, CancellationToken cancellationToken = default);
    Task PreloadAsync(int centerX, int centerZ, int radius, CancellationToken cancellationToken = default);
    WorldMapProfile GetProfile(int playerId);
    void UpdateProfile(int playerId, ProfileUpdate[] updates);
    string GetGenerationSignature();
}

public sealed class WorldMapController : IWorldController, IDisposable
{
    private readonly IWorldMapQueueManager queueManager;
    private readonly IWorldMapCacheManager cacheManager;
    private readonly IWorldMapProfileManager profileManager;
    private readonly IWorldMapGenerationCoordinator generationCoordinator;
    
    // Constructor with dependency injection
    public WorldMapController(
        IWorldMapQueueManager queueManager,
        IWorldMapCacheManager cacheManager,
        IWorldMapProfileManager profileManager,
        IWorldMapGenerationCoordinator generationCoordinator);
}
```

### Separated Components

```csharp
public interface IWorldMapQueueManager
{
    QueueState GetQueueState();
    bool ShouldDeferChunk(int chunkX, int chunkZ, int centerChunkX, int centerChunkZ, 
                        int renderDistance, QueuePressureBand pressureBand);
}

public interface IWorldMapCacheManager
{
    bool TryGetCachedChunk(int chunkX, int chunkZ, out ChunkData? chunk);
    void CacheChunk(int chunkX, int chunkZ, ChunkData chunk);
    void EnforceBudget();
}

public interface IWorldMapProfileManager
{
    WorldMapProfile GetProfile(int playerId);
    void UpdateProfile(int playerId, ProfileUpdate[] updates);
    void ReloadIfNeeded();
    string GetProfileHash();
}
```

## Conclusion

The world map control architecture has good foundations with advanced queue management and caching strategies. However, there are critical issues that need to be addressed:

1. **Critical**: Duplicate functionality between two controllers
2. **High**: Complex queue state management and lack of separation of concerns
3. **Medium**: Performance optimizations and missing client-side implementation
4. **Low**: Consistency improvements and feature additions

The proposed unified architecture with separated components will result in:
- **Better maintainability**: Clear separation of concerns
- **Better testability**: Interface abstraction and dependency injection
- **Better performance**: Optimized caching and queue management
- **Better scalability**: Async operations and efficient resource usage
- **Better user experience**: Client-side caching and movement prediction

These improvements will result in a more robust, maintainable, and performant world map control system.
**Date**: 2026-03-01  
**Session**: 137  
**Status**: In Progress

## Overview

This document reviews the world map control architecture for server and client, identifying strengths, weaknesses, and improvement opportunities.

## Files Reviewed

1. **WorldMapControlManager.cs** (1,232 lines)
   - Lightweight world map control service
   - Manages profile tracking, chunk caching, queue management
   - Supports inflight chunk generation tracking
   - Complex adaptive queue policy with pressure bands

2. **WorldMapController.cs** (809 lines)
   - Centralized world map controller
   - Manages chunk generation and caching
   - Has queue pressure management
   - Cleanup timer for old chunks

## Strengths

### 1. Advanced Queue Management
- Adaptive queue limits based on system load
- Multiple pressure bands (Critical, High, Elevated, Normal, Low)
- Emergency brake mechanism for overload protection
- Backoff and recovery ramp for smooth transitions

### 2. Profile Management
- Per-player map preferences (render distance, map scale, quality settings)
- Profile versioning and hash-based change detection
- Automatic profile reloading on config changes

### 3. Caching Strategy
- Chunk cache with access time tracking
- Inflight chunk generation tracking with timeout
- Automatic cache pruning based on budget

### 4. Data-Driven Configuration
- Extensive use of config classes
- JSON-based configuration support
- Tunable parameters for all aspects of control

### 5. Signature-Based Validation
- Generation signature for cache invalidation
- Protocol fingerprint validation
- Config hash-based reload detection

## Critical Issues

### Issue 1. Duplicate Functionality (CRITICAL)

**Problem**: Two separate classes (`WorldMapControlManager` and `WorldMapController`) implement very similar functionality with different approaches.

**Evidence**:
- Both manage chunk generation and caching
- Both have queue pressure management
- Both have cleanup timers
- Both use similar config and profile structures

**Impact**: 
- Code duplication
- Maintenance burden
- Potential inconsistency between implementations
- Confusion about which to use

**Recommendation**: Merge into single unified controller with clear separation of concerns.

### Issue 2. Inconsistent Queue Policy Implementation

**Problem**: Queue policy logic is duplicated with slight variations between the two classes.

**Examples**:
- `WorldMapControlManager.GetAdaptiveQueueLimit()` (lines 625-793)
- `WorldMapController.GetAdaptiveQueueState()` (lines 521-585)

**Impact**: Different behavior depending on which controller is used, potential bugs.

**Recommendation**: Extract queue policy to shared utility class.

### Issue 3. Magic Numbers and Thresholds

**Problem**: Hard-coded constants scattered throughout both files.

**Examples**:
- `0.55`, `0.35`, `0.25` - blending weights
- `0.88`, `0.92`, `0.98` - load shedding thresholds
- `1.15`, `1.28`, `3.0` - slack ratios
- `0.42`, `0.24`, `0.22` - various weights

**Recommendation**: Define named constants in config classes.

### Issue 4. Complex Queue State Management

**Problem**: Queue state is computed on-demand with complex logic that's hard to reason about.

**Examples**:
- `GetAdaptiveQueueState()` computes 7 different values
- Emergency latch with multiple conditions
- Recovery ramp with countdowns

**Impact**: Difficult to debug and maintain, potential edge cases.

**Recommendation**: Simplify queue state machine with clear transitions.

### Issue 5. No Client-Side Implementation

**Problem**: Only server-side implementation exists. Client-side world map control is missing.

**Impact**: Client cannot manage map preferences or cache chunks locally.

**Recommendation**: Implement client-side counterpart.

## Code Organization Issues

### Issue 6. Long Methods

**Problem**: Some methods exceed 100 lines with complex nested logic.

**Examples**:
- `WorldMapControlManager.GetAdaptiveQueueLimit()` (169 lines)
- `WorldMapControlManager.ComputeGenerationSignature()` (116 lines)
- `WorldMapController.GetAdaptiveQueueState()` (65 lines)

**Recommendation**: Extract helper methods for:
- Queue policy computation
- Signature computation
- State transitions

### Issue 7. Repeated Patterns

**Problem**: Similar calculation patterns repeated across both classes.

**Examples**:
- File hash computation
- Queue pressure classification
- Cache budget computation
- Access time tracking

**Recommendation**: Create shared utility methods.

### Issue 8. Inconsistent Naming

**Problem**: Different naming conventions for similar concepts.

**Examples**:
- `inflightChunkGenerations` vs `generationTasks`
- `chunkAccessTimes` vs `accessTimes`
- `maxCachedChunks` vs `maxLoadedChunks`

**Recommendation**: Standardize naming across both classes.

## Performance Issues

### Issue 9. Unnecessary Dictionary Lookups

**Problem**: Some dictionary lookups could be cached or avoided.

**Location**: `WorldMapControlManager.TryGetNearestPlayerChunkFocus()` (lines 545-566)

**Impact**: O(n) lookup for each chunk in hot path.

**Recommendation**: Precompute or use spatial indexing.

### Issue 10. Repeated File System Access

**Problem**: File write times are checked multiple times per request.

**Location**: `MaybeReloadGenerationConfig()` and `EnsureProfile()` both check file times

**Impact**: Unnecessary I/O operations.

**Recommendation**: Cache file metadata with change notifications.

### Issue 11. Inefficient Pruning

**Problem**: Pruning iterates over entire cache multiple times.

**Location**: `EnforceCacheBudget()` (lines 885-926) and `EnforceLoadedChunkBudget()` (lines 614-656)

**Impact**: O(n) operations where O(k) would suffice.

**Recommendation**: Use priority queue or indexed access.

## Consistency Issues

### Issue 12. Different Clamping Thresholds

**Problem**: Different clamping thresholds for similar operations.

**Examples**:
- `Math.Clamp(value, 0.0, 1.35)` in manager
- `Math.Clamp(value, 0.0, 1.0)` in controller
- `Math.Clamp(value, 0.0, 1.2)` in controller

**Recommendation**: Standardize clamping ranges.

### Issue 13. Different Hash Computation Methods

**Problem**: Two different implementations of file hash computation.

**Examples**:
- `WorldMapControlManager.ComputeFileHash()` uses SHA256
- `WorldMapController.ComputeFileHash()` uses SHA256 but different format

**Recommendation**: Use single shared implementation.

### Issue 14. Inconsistent Logging

**Problem**: Different logging patterns and levels.

**Examples**:
- Manager uses `[WorldMapControlManager]` prefix
- Controller uses `[WorldMapController]` prefix
- Different log levels for similar operations

**Recommendation**: Standardize logging format and levels.

## Architecture Issues

### Issue 15. No Clear Separation of Concerns

**Problem**: Both classes mix responsibilities:
- Queue management
- Cache management
- Profile management
- Generation coordination
- Configuration management

**Impact**: Difficult to test and maintain, unclear ownership.

**Recommendation**: Separate into focused components:
- `WorldMapQueueManager`
- `WorldMapCacheManager`
- `WorldMapProfileManager`
- `WorldMapGenerationCoordinator`

### Issue 16. No Interface Abstraction

**Problem**: No common interface for world map control.

**Impact**: Tight coupling, difficult to mock for testing.

**Recommendation**: Define `IWorldMapController` interface.

### Issue 17. No Dependency Injection

**Problem**: Dependencies are instantiated directly in constructors.

**Impact**: Difficult to test, tight coupling.

**Recommendation**: Use dependency injection.

### Issue 18. Synchronous Operations

**Problem**: Some operations are synchronous when they could be async.

**Examples**:
- File I/O operations
- Hash computations

**Impact**: Blocks threads, poor scalability.

**Recommendation**: Make all I/O async.

## Missing Features

### Issue 19. No Chunk Prioritization by Player Movement

**Problem**: No prediction of player movement for proactive chunk loading.

**Impact**: Reactive rather than proactive loading, potential lag.

**Recommendation**: Implement movement prediction and preloading.

### Issue 20. No Chunk Quality Levels

**Problem**: All chunks have same quality regardless of importance.

**Impact**: Wasted resources on distant chunks.

**Recommendation**: Implement quality levels (LOD system).

### Issue 21. No Chunk Streaming

**Problem**: Chunks are loaded all at once, not streamed.

**Impact**: Long load times for large areas.

**Recommendation**: Implement progressive streaming.

### Issue 22. No Client-Side Caching

**Problem**: No client-side chunk cache or prediction.

**Impact**: Network bandwidth waste, slower loading.

**Recommendation**: Implement client-side caching and prediction.

## Priority Recommendations

### Critical Priority
1. **Merge duplicate controllers** - Eliminates code duplication
2. **Extract queue policy to shared class** - Prevents inconsistencies
3. **Define named constants** - Improves maintainability

### High Priority
4. **Separate concerns into focused components** - Improves architecture
5. **Implement interface abstraction** - Improves testability
6. **Make I/O operations async** - Improves scalability

### Medium Priority
7. **Simplify queue state machine** - Improves debuggability
8. **Implement client-side controller** - Enables client caching
9. **Add movement prediction** - Improves loading
10. **Implement quality levels** - Optimizes resources

### Low Priority
11. **Standardize naming** - Improves consistency
12. **Optimize dictionary lookups** - Performance
13. **Cache file metadata** - Performance
14. **Implement chunk streaming** - Feature enhancement

## Implementation Plan

### Phase 1: Critical Fixes
- [ ] Merge `WorldMapControlManager` and `WorldMapController` into unified controller
- [ ] Extract queue policy to `WorldMapQueuePolicy` class
- [ ] Define all magic numbers as named constants
- [ ] Create `IWorldMapController` interface

### Phase 2: Architecture Improvements
- [ ] Separate into focused components
- [ ] Implement dependency injection
- [ ] Make all I/O async
- [ ] Standardize naming conventions

### Phase 3: Performance Optimizations
- [ ] Cache file metadata
- [ ] Optimize dictionary lookups
- [ ] Improve pruning efficiency
- [ ] Precompute terrain metrics

### Phase 4: Feature Additions
- [ ] Implement client-side controller
- [ ] Add movement prediction
- [ ] Implement quality levels
- [ ] Add chunk streaming

## Testing Strategy

### Unit Tests
- Test queue policy computation
- Test cache budget enforcement
- Test profile management
- Test signature computation

### Integration Tests
- Test controller coordination
- Verify cross-chunk continuity
- Test queue pressure handling

### Performance Tests
- Measure generation time per chunk
- Profile hotspots
- Compare before/after optimization

## Proposed Architecture

### Unified WorldMapController

```csharp
public interface IWorldMapController
{
    Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ, CancellationToken cancellationToken = default);
    Task PreloadAsync(int centerX, int centerZ, int radius, CancellationToken cancellationToken = default);
    WorldMapProfile GetProfile(int playerId);
    void UpdateProfile(int playerId, ProfileUpdate[] updates);
    string GetGenerationSignature();
}

public sealed class WorldMapController : IWorldController, IDisposable
{
    private readonly IWorldMapQueueManager queueManager;
    private readonly IWorldMapCacheManager cacheManager;
    private readonly IWorldMapProfileManager profileManager;
    private readonly IWorldMapGenerationCoordinator generationCoordinator;
    
    // Constructor with dependency injection
    public WorldMapController(
        IWorldMapQueueManager queueManager,
        IWorldMapCacheManager cacheManager,
        IWorldMapProfileManager profileManager,
        IWorldMapGenerationCoordinator generationCoordinator);
}
```

### Separated Components

```csharp
public interface IWorldMapQueueManager
{
    QueueState GetQueueState();
    bool ShouldDeferChunk(int chunkX, int chunkZ, int centerChunkX, int centerChunkZ, 
                        int renderDistance, QueuePressureBand pressureBand);
}

public interface IWorldMapCacheManager
{
    bool TryGetCachedChunk(int chunkX, int chunkZ, out ChunkData? chunk);
    void CacheChunk(int chunkX, int chunkZ, ChunkData chunk);
    void EnforceBudget();
}

public interface IWorldMapProfileManager
{
    WorldMapProfile GetProfile(int playerId);
    void UpdateProfile(int playerId, ProfileUpdate[] updates);
    void ReloadIfNeeded();
    string GetProfileHash();
}
```

## Conclusion

The world map control architecture has good foundations with advanced queue management and caching strategies. However, there are critical issues that need to be addressed:

1. **Critical**: Duplicate functionality between two controllers
2. **High**: Complex queue state management and lack of separation of concerns
3. **Medium**: Performance optimizations and missing client-side implementation
4. **Low**: Consistency improvements and feature additions

The proposed unified architecture with separated components will result in:
- **Better maintainability**: Clear separation of concerns
- **Better testability**: Interface abstraction and dependency injection
- **Better performance**: Optimized caching and queue management
- **Better scalability**: Async operations and efficient resource usage
- **Better user experience**: Client-side caching and movement prediction

These improvements will result in a more robust, maintainable, and performant world map control system.

