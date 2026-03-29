# Data-Driven Approach Improvements (데이터 기반 접근 방식 개선)

## Current Data Management Analysis (현재 데이터 관리 분석)

### Existing Data Systems (기존 데이터 시스템)
- **JSON Configuration Files** - 기본 설정 데이터
- **Database Storage** - 플레이어 및 월드 데이터
- **Hard-coded Values** - 게임 로직에 내장된 값들
- **Static Resources** - Unity 에셋 및 리소스

### Current Implementation Strengths (현재 구현 강점)
1. **Basic JSON Support** - 기본 JSON 데이터 지원
2. **Database Integration** - 데이터베이스 연동
3. **Resource Loading** - 리소스 로딩 시스템
4. **Configuration Management** - 설정 관리

### Identified Issues (식별된 문제점)
1. **Hard-coded Game Logic** - 게임 로직이 코드에 하드코딩됨
2. **Limited Data Sources** - 제한된 데이터 소스
3. **No Data Validation** - 데이터 검증 부재
4. **Static Data Only** - 정적 데이터만 지원
5. **No Runtime Data Updates** - 런타임 데이터 업데이트 부재
6. **Poor Data Organization** - 데이터 구조화 부족

## Proposed Data-Driven Architecture (제안된 데이터 기반 아키텍처)

### 1. Unified Data Management System (통합 데이터 관리 시스템)

#### 1.1 Data Repository Pattern (데이터 리포지토리 패턴)
```csharp
// IDataRepository.cs
public interface IDataRepository<T> where T : class
{
    Task<T> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> QueryAsync(Func<T, bool> predicate);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}

// JsonDataRepository.cs
public class JsonDataRepository<T> : IDataRepository<T> where T : class
{
    private readonly string _filePath;
    private readonly IDataSerializer _serializer;
    private readonly ILogger<JsonDataRepository<T>> _logger;
    private readonly SemaphoreSlim _semaphore;
    private Dictionary<string, T> _data;
    
    public JsonDataRepository(
        string filePath,
        IDataSerializer serializer,
        ILogger<JsonDataRepository<T>> logger)
    {
        _filePath = filePath;
        _serializer = serializer;
        _logger = logger;
        _semaphore = new SemaphoreSlim(1, 1);
        _data = new Dictionary<string, T>();
    }
    
    public async Task<T> GetByIdAsync(string id)
    {
        await _semaphore.WaitAsync();
        try
        {
            await LoadIfNotLoaded();
            _data.TryGetValue(id, out var entity);
            return entity;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            await LoadIfNotLoaded();
            return _data.Values.ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<T> AddAsync(T entity)
    {
        await _semaphore.WaitAsync();
        try
        {
            await LoadIfNotLoaded();
            var id = GetEntityId(entity);
            _data[id] = entity;
            await SaveAsync();
            return entity;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private async Task LoadIfNotLoaded()
    {
        if (_data.Count > 0) return;
        
        if (File.Exists(_filePath))
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var dataList = _serializer.Deserialize<List<T>>(json);
            _data = dataList.ToDictionary(GetEntityId);
        }
    }
    
    private async Task SaveAsync()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        var dataList = _data.Values.ToList();
        var json = _serializer.Serialize(dataList);
        await File.WriteAllTextAsync(_filePath, json);
    }
    
    private string GetEntityId(T entity)
    {
        var property = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("id");
        return property?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }
}

// DatabaseDataRepository.cs
public class DatabaseDataRepository<T> : IDataRepository<T> where T : class
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<DatabaseDataRepository<T>> _logger;
    
    public DatabaseDataRepository(IDbContext dbContext, ILogger<DatabaseDataRepository<T>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<T> GetByIdAsync(string id)
    {
        try
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity by id: {Id}", id);
            throw;
        }
    }
    
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return await _dbContext.Set<T>().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all entities");
            throw;
        }
    }
    
    public async Task<T> AddAsync(T entity)
    {
        try
        {
            await _dbContext.Set<T>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add entity");
            throw;
        }
    }
}
```

#### 1.2 Data Manager (데이터 관리자)
```csharp
// DataManager.cs
public class DataManager
{
    private readonly Dictionary<Type, object> _repositories;
    private readonly IDataCache _cache;
    private readonly IDataValidator _validator;
    private readonly ILogger<DataManager> _logger;
    
    public DataManager(
        IEnumerable<IDataRepository> repositories,
        IDataCache cache,
        IDataValidator validator,
        ILogger<DataManager> logger)
    {
        _repositories = repositories.ToDictionary(r => r.GetType().GetGenericArguments()[0], r => (object)r);
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }
    
    public IDataRepository<T> GetRepository<T>() where T : class
    {
        if (_repositories.TryGetValue(typeof(T), out var repository))
        {
            return (IDataRepository<T>)repository;
        }
        
        throw new InvalidOperationException($"Repository for type {typeof(T).Name} not found");
    }
    
    public async Task<T> GetAsync<T>(string id, bool useCache = true) where T : class
    {
        if (useCache)
        {
            var cached = await _cache.GetAsync<T>(id);
            if (cached != null)
            {
                return cached;
            }
        }
        
        var repository = GetRepository<T>();
        var entity = await repository.GetByIdAsync(id);
        
        if (useCache && entity != null)
        {
            await _cache.SetAsync(id, entity);
        }
        
        return entity;
    }
    
    public async Task<IEnumerable<T>> GetAllAsync<T>(bool useCache = true) where T : class
    {
        var cacheKey = $"all_{typeof(T).Name}";
        
        if (useCache)
        {
            var cached = await _cache.GetAsync<IEnumerable<T>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
        }
        
        var repository = GetRepository<T>();
        var entities = await repository.GetAllAsync();
        
        if (useCache)
        {
            await _cache.SetAsync(cacheKey, entities);
        }
        
        return entities;
    }
    
    public async Task<T> SetAsync<T>(T entity, bool validate = true) where T : class
    {
        if (validate)
        {
            var validationResult = _validator.Validate(entity);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }
        
        var repository = GetRepository<T>();
        var result = await repository.UpdateAsync(entity);
        
        // Update cache
        var id = GetEntityId(entity);
        await _cache.SetAsync(id, entity);
        
        // Invalidate list cache
        var cacheKey = $"all_{typeof(T).Name}";
        await _cache.RemoveAsync(cacheKey);
        
        return result;
    }
    
    private string GetEntityId<T>(T entity)
    {
        var property = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("id");
        return property?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }
}
```

### 2. Game Data Definitions (게임 데이터 정의)

#### 2.1 Block Data System (블록 데이터 시스템)
```csharp
// BlockData.cs
public class BlockData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public BlockType Type { get; set; }
    public BlockCategory Category { get; set; }
    public BlockProperties Properties { get; set; }
    public BlockPhysics Physics { get; set; }
    public BlockVisual Visual { get; set; }
    public BlockInteraction Interaction { get; set; }
    public BlockCrafting Crafting { get; set; }
    public Dictionary<string, object> CustomData { get; set; }
}

public class BlockProperties
{
    public int Hardness { get; set; }
    public int Resistance { get; set; }
    public bool IsSolid { get; set; }
    public bool IsTransparent { get; set; }
    public bool IsLightEmitter { get; set; }
    public int LightLevel { get; set; }
    public bool IsFlammable { get; set; }
    public bool CanBurn { get; set; }
    public float Friction { get; set; }
    public bool RequiresTool { get; set; }
    public ToolType RequiredTool { get; set; }
    public int RequiredToolLevel { get; set; }
}

public class BlockPhysics
{
    public bool HasGravity { get; set; }
    public bool CanFall { get; set; }
    public bool IsSupport { get; set; }
    public bool CanSupport { get; set; }
    public float Density { get; set; }
    public bool IsLiquid { get; set; }
    public LiquidProperties Liquid { get; set; }
}

public class BlockVisual
{
    public string TexturePath { get; set; }
    public string ModelPath { get; set; }
    public Vector3 Scale { get; set; }
    public Vector3 Rotation { get; set; }
    public bool IsAnimated { get; set; }
    public AnimationData Animation { get; set; }
    public ParticleEffect ParticleEffect { get; set; }
    public SoundEffect SoundEffect { get; set; }
}

public class BlockInteraction
{
    public bool CanInteract { get; set; }
    public InteractionType[] Interactions { get; set; }
    public DropTable DropTable { get; set; }
    public ExperienceReward Experience { get; set; }
    public Dictionary<string, object> InteractionData { get; set; }
}

// blocks.json - Block data definitions
{
  "blocks": [
    {
      "id": "minecraft:stone",
      "name": "stone",
      "displayName": "Stone",
      "type": "solid",
      "category": "natural",
      "properties": {
        "hardness": 3,
        "resistance": 6,
        "isSolid": true,
        "isTransparent": false,
        "isLightEmitter": false,
        "lightLevel": 0,
        "isFlammable": false,
        "canBurn": false,
        "friction": 0.6,
        "requiresTool": true,
        "requiredTool": "pickaxe",
        "requiredToolLevel": 0
      },
      "physics": {
        "hasGravity": false,
        "canFall": false,
        "isSupport": true,
        "canSupport": true,
        "density": 2.5,
        "isLiquid": false
      },
      "visual": {
        "texturePath": "blocks/stone",
        "modelPath": "blocks/cube",
        "scale": { "x": 1, "y": 1, "z": 1 },
        "rotation": { "x": 0, "y": 0, "z": 0 },
        "isAnimated": false,
        "particleEffect": null,
        "soundEffect": {
          "breakSound": "block.stone.break",
          "placeSound": "block.stone.place",
          "stepSound": "block.stone.step"
        }
      },
      "interaction": {
        "canInteract": true,
        "interactions": ["break", "place"],
        "dropTable": "stone_drop_table",
        "experience": { "min": 0, "max": 3 }
      }
    },
    {
      "id": "minecraft:dirt",
      "name": "dirt",
      "displayName": "Dirt",
      "type": "solid",
      "category": "natural",
      "properties": {
        "hardness": 1.5,
        "resistance": 1.25,
        "isSolid": true,
        "isTransparent": false,
        "isLightEmitter": false,
        "lightLevel": 0,
        "isFlammable": false,
        "canBurn": false,
        "friction": 0.6,
        "requiresTool": false,
        "requiredTool": null,
        "requiredToolLevel": 0
      },
      "physics": {
        "hasGravity": false,
        "canFall": false,
        "isSupport": true,
        "canSupport": true,
        "density": 1.2,
        "isLiquid": false
      },
      "visual": {
        "texturePath": "blocks/dirt",
        "modelPath": "blocks/cube",
        "scale": { "x": 1, "y": 1, "z": 1 },
        "rotation": { "x": 0, "y": 0, "z": 0 },
        "isAnimated": false
      },
      "interaction": {
        "canInteract": true,
        "interactions": ["break", "place"],
        "dropTable": "dirt_drop_table"
      }
    }
  ]
}
```

#### 2.2 Item Data System (아이템 데이터 시스템)
```csharp
// ItemData.cs
public class ItemData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public ItemType Type { get; set; }
    public ItemCategory Category { get; set; }
    public ItemProperties Properties { get; set; }
    public ItemUsage Usage { get; set; }
    public ItemCrafting Crafting { get; set; }
    public ItemVisual Visual { get; set; }
    public Dictionary<string, object> CustomData { get; set; }
}

public class ItemProperties
{
    public int MaxStackSize { get; set; }
    public int MaxDurability { get; set; }
    public bool IsDamageable { get; set; }
    public bool IsEnchantable { get; set; }
    public int MaxEnchantmentLevel { get; set; }
    public bool IsStackable { get; set; }
    public float Weight { get; set; }
    public bool IsConsumable { get; set; }
    public int ConsumptionTime { get; set; }
    public Effect[] Effects { get; set; }
}

public class ItemUsage
{
    public bool CanUse { get; set; }
    public UsageType[] UsageTypes { get; set; }
    public float UseDuration { get; set; }
    public float Cooldown { get; set; }
    public Effect[] OnUseEffects { get; set; }
    public Dictionary<string, object> UsageData { get; set; }
}

public class ItemCrafting
{
    public bool CanCraft { get; set; }
    public Recipe[] Recipes { get; set; }
    public CraftingStation RequiredStation { get; set; }
    public int CraftingLevel { get; set; }
    public Dictionary<string, int> Materials { get; set; }
}

// items.json - Item data definitions
{
  "items": [
    {
      "id": "minecraft:wooden_pickaxe",
      "name": "wooden_pickaxe",
      "displayName": "Wooden Pickaxe",
      "type": "tool",
      "category": "tools",
      "properties": {
        "maxStackSize": 1,
        "maxDurability": 59,
        "isDamageable": true,
        "isEnchantable": true,
        "maxEnchantmentLevel": 15,
        "isStackable": false,
        "weight": 1.0,
        "isConsumable": false
      },
      "usage": {
        "canUse": true,
        "usageTypes": ["mine", "attack"],
        "useDuration": 0.1,
        "cooldown": 0.0
      },
      "crafting": {
        "canCraft": true,
        "requiredStation": "crafting_table",
        "craftingLevel": 1,
        "materials": {
          "minecraft:wood_planks": 3,
          "minecraft:stick": 2
        }
      },
      "visual": {
        "texturePath": "items/wooden_pickaxe",
        "modelPath": "items/pickaxe",
        "scale": { "x": 1, "y": 1, "z": 1 }
      }
    }
  ]
}
```

#### 2.3 Recipe Data System (레시피 데이터 시스템)
```csharp
// RecipeData.cs
public class RecipeData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public RecipeType Type { get; set; }
    public CraftingStation Station { get; set; }
    public RecipeInput[] Inputs { get; set; }
    public RecipeOutput[] Outputs { get; set; }
    public int CraftingTime { get; set; }
    public int RequiredLevel { get; set; }
    public Dictionary<string, object> Conditions { get; set; }
}

public class RecipeInput
{
    public string ItemId { get; set; }
    public int Count { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public bool IsOptional { get; set; }
}

public class RecipeOutput
{
    public string ItemId { get; set; }
    public int Count { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public float Probability { get; set; }
}

// recipes.json - Recipe data definitions
{
  "recipes": [
    {
      "id": "minecraft:wooden_pickaxe_crafting",
      "name": "Wooden Pickaxe",
      "type": "shaped",
      "station": "crafting_table",
      "inputs": [
        { "itemId": "minecraft:wood_planks", "count": 1, "position": [0, 0] },
        { "itemId": "minecraft:wood_planks", "count": 1, "position": [1, 0] },
        { "itemId": "minecraft:wood_planks", "count": 1, "position": [2, 0] },
        { "itemId": null, "count": 0, "position": [0, 1] },
        { "itemId": "minecraft:stick", "count": 1, "position": [1, 1] },
        { "itemId": null, "count": 0, "position": [2, 1] },
        { "itemId": null, "count": 0, "position": [0, 2] },
        { "itemId": "minecraft:stick", "count": 1, "position": [1, 2] },
        { "itemId": null, "count": 0, "position": [2, 2] }
      ],
      "outputs": [
        { "itemId": "minecraft:wooden_pickaxe", "count": 1, "probability": 1.0 }
      ],
      "craftingTime": 0,
      "requiredLevel": 1
    }
  ]
}
```

### 3. Dynamic Data Loading System (동적 데이터 로딩 시스템)

#### 3.1 Data Loader (데이터 로더)
```csharp
// IDataLoader.cs
public interface IDataLoader
{
    Task<T> LoadAsync<T>(string path) where T : class;
    Task<IEnumerable<T>> LoadCollectionAsync<T>(string path) where T : class;
    Task<bool> SaveAsync<T>(string path, T data) where T : class;
    Task<bool> SaveCollectionAsync<T>(string path, IEnumerable<T> data) where T : class;
    event EventHandler<DataLoadedEventArgs> DataLoaded;
    event EventHandler<DataSavedEventArgs> DataSaved;
}

// JsonDataLoader.cs
public class JsonDataLoader : IDataLoader
{
    private readonly IDataSerializer _serializer;
    private readonly ILogger<JsonDataLoader> _logger;
    
    public JsonDataLoader(IDataSerializer serializer, ILogger<JsonDataLoader> logger)
    {
        _serializer = serializer;
        _logger = logger;
    }
    
    public async Task<T> LoadAsync<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("Data file not found: {Path}", path);
                return null;
            }
            
            var json = await File.ReadAllTextAsync(path);
            var data = _serializer.Deserialize<T>(json);
            
            DataLoaded?.Invoke(this, new DataLoadedEventArgs { Path = path, Data = data });
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load data from {Path}", path);
            throw;
        }
    }
    
    public async Task<IEnumerable<T>> LoadCollectionAsync<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("Data file not found: {Path}", path);
                return Enumerable.Empty<T>();
            }
            
            var json = await File.ReadAllTextAsync(path);
            var data = _serializer.Deserialize<IEnumerable<T>>(json);
            
            DataLoaded?.Invoke(this, new DataLoadedEventArgs { Path = path, Data = data });
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load collection from {Path}", path);
            throw;
        }
    }
    
    public async Task<bool> SaveAsync<T>(string path, T data) where T : class
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = _serializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
            
            DataSaved?.Invoke(this, new DataSavedEventArgs { Path = path, Data = data });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save data to {Path}", path);
            return false;
        }
    }
}

// HotReloadDataLoader.cs
public class HotReloadDataLoader : IDataLoader
{
    private readonly IDataLoader _innerLoader;
    private readonly Dictionary<string, FileSystemWatcher> _watchers;
    private readonly ILogger<HotReloadDataLoader> _logger;
    
    public HotReloadDataLoader(IDataLoader innerLoader, ILogger<HotReloadDataLoader> logger)
    {
        _innerLoader = innerLoader;
        _watchers = new Dictionary<string, FileSystemWatcher>();
        _logger = logger;
    }
    
    public async Task<T> LoadAsync<T>(string path) where T : class
    {
        SetupWatcher(path);
        return await _innerLoader.LoadAsync<T>(path);
    }
    
    public async Task<IEnumerable<T>> LoadCollectionAsync<T>(string path) where T : class
    {
        SetupWatcher(path);
        return await _innerLoader.LoadCollectionAsync<T>(path);
    }
    
    private void SetupWatcher(string path)
    {
        if (_watchers.ContainsKey(path)) return;
        
        var directory = Path.GetDirectoryName(path);
        var filename = Path.GetFileName(path);
        
        var watcher = new FileSystemWatcher(directory, filename);
        watcher.Changed += async (sender, e) =>
        {
            try
            {
                await Task.Delay(100); // Wait for file write to complete
                _logger.LogInformation("Data file changed: {Path}", path);
                
                // Trigger reload event
                DataLoaded?.Invoke(this, new DataLoadedEventArgs { Path = path });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle file change for {Path}", path);
            }
        };
        
        watcher.EnableRaisingEvents = true;
        _watchers[path] = watcher;
    }
    
    public event EventHandler<DataLoadedEventArgs> DataLoaded;
    public event EventHandler<DataSavedEventArgs> DataSaved;
}
```

### 4. Data Validation System (데이터 검증 시스템)

#### 4.1 Data Validators (데이터 검증기)
```csharp
// IDataValidator.cs
public interface IDataValidator
{
    ValidationResult Validate<T>(T data);
    ValidationResult ValidateCollection<T>(IEnumerable<T> data);
}

// BlockDataValidator.cs
public class BlockDataValidator : IDataValidator
{
    public ValidationResult Validate<T>(T data)
    {
        if (data is not BlockData blockData)
        {
            return new ValidationResult { IsValid = false, Errors = ["Data is not BlockData"] };
        }
        
        var errors = new List<string>();
        var warnings = new List<string>();
        
        // Validate required fields
        if (string.IsNullOrEmpty(blockData.Id))
            errors.Add("Block ID is required");
        
        if (string.IsNullOrEmpty(blockData.Name))
            errors.Add("Block name is required");
        
        // Validate properties
        if (blockData.Properties != null)
        {
            if (blockData.Properties.Hardness < 0)
                errors.Add("Block hardness cannot be negative");
            
            if (blockData.Properties.Resistance < 0)
                errors.Add("Block resistance cannot be negative");
            
            if (blockData.Properties.LightLevel < 0 || blockData.Properties.LightLevel > 15)
                errors.Add("Block light level must be between 0 and 15");
        }
        
        // Validate physics
        if (blockData.Physics != null)
        {
            if (blockData.Physics.Density < 0)
                errors.Add("Block density cannot be negative");
        }
        
        // Validate texture path
        if (!string.IsNullOrEmpty(blockData.Visual?.TexturePath))
        {
            if (!File.Exists($"Assets/{blockData.Visual.TexturePath}.png"))
                warnings.Add($"Texture file not found: {blockData.Visual.TexturePath}");
        }
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
    
    public ValidationResult ValidateCollection<T>(IEnumerable<T> data)
    {
        if (data is not IEnumerable<BlockData> blockDataList)
        {
            return new ValidationResult { IsValid = false, Errors = ["Data is not BlockData collection"] };
        }
        
        var allErrors = new List<string>();
        var allWarnings = new List<string>();
        var ids = new HashSet<string>();
        
        foreach (var blockData in blockDataList)
        {
            var result = Validate(blockData);
            allErrors.AddRange(result.Errors);
            allWarnings.AddRange(result.Warnings);
            
            // Check for duplicate IDs
            if (!string.IsNullOrEmpty(blockData.Id))
            {
                if (ids.Contains(blockData.Id))
                    allErrors.Add($"Duplicate block ID: {blockData.Id}");
                else
                    ids.Add(blockData.Id);
            }
        }
        
        return new ValidationResult
        {
            IsValid = allErrors.Count == 0,
            Errors = allErrors,
            Warnings = allWarnings
        };
    }
}
```

### 5. Data-Driven Game Logic (데이터 기반 게임 로직)

#### 5.1 Data-Driven Block System (데이터 기반 블록 시스템)
```csharp
// DataDrivenBlockManager.cs
public class DataDrivenBlockManager
{
    private readonly IDataRepository<BlockData> _blockRepository;
    private readonly IDataCache _cache;
    private readonly ILogger<DataDrivenBlockManager> _logger;
    private readonly Dictionary<string, BlockData> _blockDataCache;
    
    public DataDrivenBlockManager(
        IDataRepository<BlockData> blockRepository,
        IDataCache cache,
        ILogger<DataDrivenBlockManager> _logger)
    {
        _blockRepository = blockRepository;
        _cache = cache;
        _logger = logger;
        _blockDataCache = new Dictionary<string, BlockData>();
    }
    
    public async Task<BlockData> GetBlockDataAsync(string blockId)
    {
        if (_blockDataCache.TryGetValue(blockId, out var cachedData))
        {
            return cachedData;
        }
        
        var blockData = await _blockRepository.GetByIdAsync(blockId);
        if (blockData != null)
        {
            _blockDataCache[blockId] = blockData;
        }
        
        return blockData;
    }
    
    public async Task<bool> CanBreakBlockAsync(string blockId, string playerId, ToolData tool = null)
    {
        var blockData = await GetBlockDataAsync(blockId);
        if (blockData == null) return false;
        
        // Check if block can be broken
        if (!blockData.Interaction.CanInteract)
            return false;
        
        if (!blockData.Interaction.Interactions.Contains("break"))
            return false;
        
        // Check tool requirements
        if (blockData.Properties.RequiresTool)
        {
            if (tool == null) return false;
            
            if (tool.Type != blockData.Properties.RequiredTool)
                return false;
            
            if (tool.Level < blockData.Properties.RequiredToolLevel)
                return false;
        }
        
        return true;
    }
    
    public async Task<ItemStack[]> GetBlockDropsAsync(string blockId, ToolData tool = null)
    {
        var blockData = await GetBlockDataAsync(blockId);
        if (blockData?.Interaction?.DropTable == null)
        {
            return Array.Empty<ItemStack>();
        }
        
        var dropTable = await GetDropTableAsync(blockData.Interaction.DropTable);
        if (dropTable == null)
        {
            return Array.Empty<ItemStack>();
        }
        
        return CalculateDrops(dropTable, tool);
    }
    
    public async Task<int> GetBlockBreakExperienceAsync(string blockId)
    {
        var blockData = await GetBlockDataAsync(blockId);
        if (blockData?.Interaction?.Experience == null)
        {
            return 0;
        }
        
        var exp = blockData.Interaction.Experience;
        return Random.Shared.Next(exp.Min, exp.Max + 1);
    }
    
    private async Task<DropTableData> GetDropTableAsync(string dropTableId)
    {
        // Implementation for loading drop table data
        return null;
    }
    
    private ItemStack[] CalculateDrops(DropTableData dropTable, ToolData tool)
    {
        // Implementation for calculating drops based on drop table and tool
        return Array.Empty<ItemStack>();
    }
}
```

### 6. External Data Integration (외부 데이터 통합)

#### 6.1 API Data Provider (API 데이터 제공자)
```csharp
// IExternalDataProvider.cs
public interface IExternalDataProvider
{
    Task<T> GetDataAsync<T>(string endpoint, Dictionary<string, string> parameters = null);
    Task<bool> SendDataAsync<T>(string endpoint, T data);
    event EventHandler<ExternalDataEventArgs> DataReceived;
    event EventHandler<ExternalDataEventArgs> DataSent;
}

// RestApiDataProvider.cs
public class RestApiDataProvider : IExternalDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RestApiDataProvider> _logger;
    private readonly string _baseUrl;
    
    public RestApiDataProvider(HttpClient httpClient, ILogger<RestApiDataProvider> logger, string baseUrl)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = baseUrl;
    }
    
    public async Task<T> GetDataAsync<T>(string endpoint, Dictionary<string, string> parameters = null)
    {
        try
        {
            var url = $"{_baseUrl}/{endpoint}";
            if (parameters != null && parameters.Count > 0)
            {
                var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
                url += $"?{queryString}";
            }
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<T>(json);
            
            DataReceived?.Invoke(this, new ExternalDataEventArgs { Endpoint = endpoint, Data = data });
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data from {Endpoint}", endpoint);
            throw;
        }
    }
    
    public async Task<bool> SendDataAsync<T>(string endpoint, T data)
    {
        try
        {
            var url = $"{_baseUrl}/{endpoint}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            var success = response.IsSuccessStatusCode;
            
            if (success)
            {
                DataSent?.Invoke(this, new ExternalDataEventArgs { Endpoint = endpoint, Data = data });
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send data to {Endpoint}", endpoint);
            return false;
        }
    }
}
```

## Implementation Plan (구현 계획)

### Phase 1: Core Data Infrastructure (핵심 데이터 인프라)
1. **Data Repository Pattern** 구현
2. **Data Manager** 개발
3. **Data Loader** 시스템 구축
4. **Data Validation** 검증 시스템

### Phase 2: Game Data Systems (게임 데이터 시스템)
1. **Block Data System** 블록 데이터 시스템
2. **Item Data System** 아이템 데이터 시스템
3. **Recipe Data System** 레시피 데이터 시스템
4. **Drop Table System** 드롭 테이블 시스템

### Phase 3: Dynamic Loading (동적 로딩)
1. **Hot Reload** 핫 리로드 구현
2. **External Data** 외부 데이터 통합
3. **Cache System** 캐시 시스템
4. **Performance** 성능 최적화

### Phase 4: Integration (통합)
1. **Game Logic Integration** 게임 로직 통합
2. **UI Integration** UI 통합
3. **Testing** 테스트
4. **Documentation** 문서화

## Expected Benefits (기대 효과)

### Development Benefits (개발상 이점)
- **Faster Development**: 빠른 개발 속도
- **Easier Balancing**: 쉬운 밸런싱
- **Modular Design**: 모듈식 디자인
- **Better Testing**: 향상된 테스트

### Operational Benefits (운영상 이점)
- **Hot Updates**: 핫 업데이트 지원
- **Remote Configuration**: 원격 설정
- **Better Analytics**: 향상된 분석
- **Easier Debugging**: 쉬운 디버깅

### User Experience Benefits (사용자 경험 이점)
- **Dynamic Content**: 동적 콘텐츠
- **Personalization**: 개인화
- **Real-time Updates**: 실시간 업데이트
- **Consistent Experience**: 일관된 경험
## Current Data Management Analysis (현재 데이터 관리 분석)

### Existing Data Systems (기존 데이터 시스템)
- **JSON Configuration Files** - 기본 설정 데이터
- **Database Storage** - 플레이어 및 월드 데이터
- **Hard-coded Values** - 게임 로직에 내장된 값들
- **Static Resources** - Unity 에셋 및 리소스

### Current Implementation Strengths (현재 구현 강점)
1. **Basic JSON Support** - 기본 JSON 데이터 지원
2. **Database Integration** - 데이터베이스 연동
3. **Resource Loading** - 리소스 로딩 시스템
4. **Configuration Management** - 설정 관리

### Identified Issues (식별된 문제점)
1. **Hard-coded Game Logic** - 게임 로직이 코드에 하드코딩됨
2. **Limited Data Sources** - 제한된 데이터 소스
3. **No Data Validation** - 데이터 검증 부재
4. **Static Data Only** - 정적 데이터만 지원
5. **No Runtime Data Updates** - 런타임 데이터 업데이트 부재
6. **Poor Data Organization** - 데이터 구조화 부족

## Proposed Data-Driven Architecture (제안된 데이터 기반 아키텍처)

### 1. Unified Data Management System (통합 데이터 관리 시스템)

#### 1.1 Data Repository Pattern (데이터 리포지토리 패턴)
```csharp
// IDataRepository.cs
public interface IDataRepository<T> where T : class
{
    Task<T> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> QueryAsync(Func<T, bool> predicate);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}

// JsonDataRepository.cs
public class JsonDataRepository<T> : IDataRepository<T> where T : class
{
    private readonly string _filePath;
    private readonly IDataSerializer _serializer;
    private readonly ILogger<JsonDataRepository<T>> _logger;
    private readonly SemaphoreSlim _semaphore;
    private Dictionary<string, T> _data;
    
    public JsonDataRepository(
        string filePath,
        IDataSerializer serializer,
        ILogger<JsonDataRepository<T>> logger)
    {
        _filePath = filePath;
        _serializer = serializer;
        _logger = logger;
        _semaphore = new SemaphoreSlim(1, 1);
        _data = new Dictionary<string, T>();
    }
    
    public async Task<T> GetByIdAsync(string id)
    {
        await _semaphore.WaitAsync();
        try
        {
            await LoadIfNotLoaded();
            _data.TryGetValue(id, out var entity);
            return entity;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            await LoadIfNotLoaded();
            return _data.Values.ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<T> AddAsync(T entity)
    {
        await _semaphore.WaitAsync();
        try
        {
            await LoadIfNotLoaded();
            var id = GetEntityId(entity);
            _data[id] = entity;
            await SaveAsync();
            return entity;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private async Task LoadIfNotLoaded()
    {
        if (_data.Count > 0) return;
        
        if (File.Exists(_filePath))
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var dataList = _serializer.Deserialize<List<T>>(json);
            _data = dataList.ToDictionary(GetEntityId);
        }
    }
    
    private async Task SaveAsync()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        var dataList = _data.Values.ToList();
        var json = _serializer.Serialize(dataList);
        await File.WriteAllTextAsync(_filePath, json);
    }
    
    private string GetEntityId(T entity)
    {
        var property = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("id");
        return property?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }
}

// DatabaseDataRepository.cs
public class DatabaseDataRepository<T> : IDataRepository<T> where T : class
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<DatabaseDataRepository<T>> _logger;
    
    public DatabaseDataRepository(IDbContext dbContext, ILogger<DatabaseDataRepository<T>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<T> GetByIdAsync(string id)
    {
        try
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity by id: {Id}", id);
            throw;
        }
    }
    
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return await _dbContext.Set<T>().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all entities");
            throw;
        }
    }
    
    public async Task<T> AddAsync(T entity)
    {
        try
        {
            await _dbContext.Set<T>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add entity");
            throw;
        }
    }
}
```

#### 1.2 Data Manager (데이터 관리자)
```csharp
// DataManager.cs
public class DataManager
{
    private readonly Dictionary<Type, object> _repositories;
    private readonly IDataCache _cache;
    private readonly IDataValidator _validator;
    private readonly ILogger<DataManager> _logger;
    
    public DataManager(
        IEnumerable<IDataRepository> repositories,
        IDataCache cache,
        IDataValidator validator,
        ILogger<DataManager> logger)
    {
        _repositories = repositories.ToDictionary(r => r.GetType().GetGenericArguments()[0], r => (object)r);
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }
    
    public IDataRepository<T> GetRepository<T>() where T : class
    {
        if (_repositories.TryGetValue(typeof(T), out var repository))
        {
            return (IDataRepository<T>)repository;
        }
        
        throw new InvalidOperationException($"Repository for type {typeof(T).Name} not found");
    }
    
    public async Task<T> GetAsync<T>(string id, bool useCache = true) where T : class
    {
        if (useCache)
        {
            var cached = await _cache.GetAsync<T>(id);
            if (cached != null)
            {
                return cached;
            }
        }
        
        var repository = GetRepository<T>();
        var entity = await repository.GetByIdAsync(id);
        
        if (useCache && entity != null)
        {
            await _cache.SetAsync(id, entity);
        }
        
        return entity;
    }
    
    public async Task<IEnumerable<T>> GetAllAsync<T>(bool useCache = true) where T : class
    {
        var cacheKey = $"all_{typeof(T).Name}";
        
        if (useCache)
        {
            var cached = await _cache.GetAsync<IEnumerable<T>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
        }
        
        var repository = GetRepository<T>();
        var entities = await repository.GetAllAsync();
        
        if (useCache)
        {
            await _cache.SetAsync(cacheKey, entities);
        }
        
        return entities;
    }
    
    public async Task<T> SetAsync<T>(T entity, bool validate = true) where T : class
    {
        if (validate)
        {
            var validationResult = _validator.Validate(entity);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }
        
        var repository = GetRepository<T>();
        var result = await repository.UpdateAsync(entity);
        
        // Update cache
        var id = GetEntityId(entity);
        await _cache.SetAsync(id, entity);
        
        // Invalidate list cache
        var cacheKey = $"all_{typeof(T).Name}";
        await _cache.RemoveAsync(cacheKey);
        
        return result;
    }
    
    private string GetEntityId<T>(T entity)
    {
        var property = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("id");
        return property?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }
}
```

### 2. Game Data Definitions (게임 데이터 정의)

#### 2.1 Block Data System (블록 데이터 시스템)
```csharp
// BlockData.cs
public class BlockData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public BlockType Type { get; set; }
    public BlockCategory Category { get; set; }
    public BlockProperties Properties { get; set; }
    public BlockPhysics Physics { get; set; }
    public BlockVisual Visual { get; set; }
    public BlockInteraction Interaction { get; set; }
    public BlockCrafting Crafting { get; set; }
    public Dictionary<string, object> CustomData { get; set; }
}

public class BlockProperties
{
    public int Hardness { get; set; }
    public int Resistance { get; set; }
    public bool IsSolid { get; set; }
    public bool IsTransparent { get; set; }
    public bool IsLightEmitter { get; set; }
    public int LightLevel { get; set; }
    public bool IsFlammable { get; set; }
    public bool CanBurn { get; set; }
    public float Friction { get; set; }
    public bool RequiresTool { get; set; }
    public ToolType RequiredTool { get; set; }
    public int RequiredToolLevel { get; set; }
}

public class BlockPhysics
{
    public bool HasGravity { get; set; }
    public bool CanFall { get; set; }
    public bool IsSupport { get; set; }
    public bool CanSupport { get; set; }
    public float Density { get; set; }
    public bool IsLiquid { get; set; }
    public LiquidProperties Liquid { get; set; }
}

public class BlockVisual
{
    public string TexturePath { get; set; }
    public string ModelPath { get; set; }
    public Vector3 Scale { get; set; }
    public Vector3 Rotation { get; set; }
    public bool IsAnimated { get; set; }
    public AnimationData Animation { get; set; }
    public ParticleEffect ParticleEffect { get; set; }
    public SoundEffect SoundEffect { get; set; }
}

public class BlockInteraction
{
    public bool CanInteract { get; set; }
    public InteractionType[] Interactions { get; set; }
    public DropTable DropTable { get; set; }
    public ExperienceReward Experience { get; set; }
    public Dictionary<string, object> InteractionData { get; set; }
}

// blocks.json - Block data definitions
{
  "blocks": [
    {
      "id": "minecraft:stone",
      "name": "stone",
      "displayName": "Stone",
      "type": "solid",
      "category": "natural",
      "properties": {
        "hardness": 3,
        "resistance": 6,
        "isSolid": true,
        "isTransparent": false,
        "isLightEmitter": false,
        "lightLevel": 0,
        "isFlammable": false,
        "canBurn": false,
        "friction": 0.6,
        "requiresTool": true,
        "requiredTool": "pickaxe",
        "requiredToolLevel": 0
      },
      "physics": {
        "hasGravity": false,
        "canFall": false,
        "isSupport": true,
        "canSupport": true,
        "density": 2.5,
        "isLiquid": false
      },
      "visual": {
        "texturePath": "blocks/stone",
        "modelPath": "blocks/cube",
        "scale": { "x": 1, "y": 1, "z": 1 },
        "rotation": { "x": 0, "y": 0, "z": 0 },
        "isAnimated": false,
        "particleEffect": null,
        "soundEffect": {
          "breakSound": "block.stone.break",
          "placeSound": "block.stone.place",
          "stepSound": "block.stone.step"
        }
      },
      "interaction": {
        "canInteract": true,
        "interactions": ["break", "place"],
        "dropTable": "stone_drop_table",
        "experience": { "min": 0, "max": 3 }
      }
    },
    {
      "id": "minecraft:dirt",
      "name": "dirt",
      "displayName": "Dirt",
      "type": "solid",
      "category": "natural",
      "properties": {
        "hardness": 1.5,
        "resistance": 1.25,
        "isSolid": true,
        "isTransparent": false,
        "isLightEmitter": false,
        "lightLevel": 0,
        "isFlammable": false,
        "canBurn": false,
        "friction": 0.6,
        "requiresTool": false,
        "requiredTool": null,
        "requiredToolLevel": 0
      },
      "physics": {
        "hasGravity": false,
        "canFall": false,
        "isSupport": true,
        "canSupport": true,
        "density": 1.2,
        "isLiquid": false
      },
      "visual": {
        "texturePath": "blocks/dirt",
        "modelPath": "blocks/cube",
        "scale": { "x": 1, "y": 1, "z": 1 },
        "rotation": { "x": 0, "y": 0, "z": 0 },
        "isAnimated": false
      },
      "interaction": {
        "canInteract": true,
        "interactions": ["break", "place"],
        "dropTable": "dirt_drop_table"
      }
    }
  ]
}
```

#### 2.2 Item Data System (아이템 데이터 시스템)
```csharp
// ItemData.cs
public class ItemData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public ItemType Type { get; set; }
    public ItemCategory Category { get; set; }
    public ItemProperties Properties { get; set; }
    public ItemUsage Usage { get; set; }
    public ItemCrafting Crafting { get; set; }
    public ItemVisual Visual { get; set; }
    public Dictionary<string, object> CustomData { get; set; }
}

public class ItemProperties
{
    public int MaxStackSize { get; set; }
    public int MaxDurability { get; set; }
    public bool IsDamageable { get; set; }
    public bool IsEnchantable { get; set; }
    public int MaxEnchantmentLevel { get; set; }
    public bool IsStackable { get; set; }
    public float Weight { get; set; }
    public bool IsConsumable { get; set; }
    public int ConsumptionTime { get; set; }
    public Effect[] Effects { get; set; }
}

public class ItemUsage
{
    public bool CanUse { get; set; }
    public UsageType[] UsageTypes { get; set; }
    public float UseDuration { get; set; }
    public float Cooldown { get; set; }
    public Effect[] OnUseEffects { get; set; }
    public Dictionary<string, object> UsageData { get; set; }
}

public class ItemCrafting
{
    public bool CanCraft { get; set; }
    public Recipe[] Recipes { get; set; }
    public CraftingStation RequiredStation { get; set; }
    public int CraftingLevel { get; set; }
    public Dictionary<string, int> Materials { get; set; }
}

// items.json - Item data definitions
{
  "items": [
    {
      "id": "minecraft:wooden_pickaxe",
      "name": "wooden_pickaxe",
      "displayName": "Wooden Pickaxe",
      "type": "tool",
      "category": "tools",
      "properties": {
        "maxStackSize": 1,
        "maxDurability": 59,
        "isDamageable": true,
        "isEnchantable": true,
        "maxEnchantmentLevel": 15,
        "isStackable": false,
        "weight": 1.0,
        "isConsumable": false
      },
      "usage": {
        "canUse": true,
        "usageTypes": ["mine", "attack"],
        "useDuration": 0.1,
        "cooldown": 0.0
      },
      "crafting": {
        "canCraft": true,
        "requiredStation": "crafting_table",
        "craftingLevel": 1,
        "materials": {
          "minecraft:wood_planks": 3,
          "minecraft:stick": 2
        }
      },
      "visual": {
        "texturePath": "items/wooden_pickaxe",
        "modelPath": "items/pickaxe",
        "scale": { "x": 1, "y": 1, "z": 1 }
      }
    }
  ]
}
```

#### 2.3 Recipe Data System (레시피 데이터 시스템)
```csharp
// RecipeData.cs
public class RecipeData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public RecipeType Type { get; set; }
    public CraftingStation Station { get; set; }
    public RecipeInput[] Inputs { get; set; }
    public RecipeOutput[] Outputs { get; set; }
    public int CraftingTime { get; set; }
    public int RequiredLevel { get; set; }
    public Dictionary<string, object> Conditions { get; set; }
}

public class RecipeInput
{
    public string ItemId { get; set; }
    public int Count { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public bool IsOptional { get; set; }
}

public class RecipeOutput
{
    public string ItemId { get; set; }
    public int Count { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public float Probability { get; set; }
}

// recipes.json - Recipe data definitions
{
  "recipes": [
    {
      "id": "minecraft:wooden_pickaxe_crafting",
      "name": "Wooden Pickaxe",
      "type": "shaped",
      "station": "crafting_table",
      "inputs": [
        { "itemId": "minecraft:wood_planks", "count": 1, "position": [0, 0] },
        { "itemId": "minecraft:wood_planks", "count": 1, "position": [1, 0] },
        { "itemId": "minecraft:wood_planks", "count": 1, "position": [2, 0] },
        { "itemId": null, "count": 0, "position": [0, 1] },
        { "itemId": "minecraft:stick", "count": 1, "position": [1, 1] },
        { "itemId": null, "count": 0, "position": [2, 1] },
        { "itemId": null, "count": 0, "position": [0, 2] },
        { "itemId": "minecraft:stick", "count": 1, "position": [1, 2] },
        { "itemId": null, "count": 0, "position": [2, 2] }
      ],
      "outputs": [
        { "itemId": "minecraft:wooden_pickaxe", "count": 1, "probability": 1.0 }
      ],
      "craftingTime": 0,
      "requiredLevel": 1
    }
  ]
}
```

### 3. Dynamic Data Loading System (동적 데이터 로딩 시스템)

#### 3.1 Data Loader (데이터 로더)
```csharp
// IDataLoader.cs
public interface IDataLoader
{
    Task<T> LoadAsync<T>(string path) where T : class;
    Task<IEnumerable<T>> LoadCollectionAsync<T>(string path) where T : class;
    Task<bool> SaveAsync<T>(string path, T data) where T : class;
    Task<bool> SaveCollectionAsync<T>(string path, IEnumerable<T> data) where T : class;
    event EventHandler<DataLoadedEventArgs> DataLoaded;
    event EventHandler<DataSavedEventArgs> DataSaved;
}

// JsonDataLoader.cs
public class JsonDataLoader : IDataLoader
{
    private readonly IDataSerializer _serializer;
    private readonly ILogger<JsonDataLoader> _logger;
    
    public JsonDataLoader(IDataSerializer serializer, ILogger<JsonDataLoader> logger)
    {
        _serializer = serializer;
        _logger = logger;
    }
    
    public async Task<T> LoadAsync<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("Data file not found: {Path}", path);
                return null;
            }
            
            var json = await File.ReadAllTextAsync(path);
            var data = _serializer.Deserialize<T>(json);
            
            DataLoaded?.Invoke(this, new DataLoadedEventArgs { Path = path, Data = data });
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load data from {Path}", path);
            throw;
        }
    }
    
    public async Task<IEnumerable<T>> LoadCollectionAsync<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("Data file not found: {Path}", path);
                return Enumerable.Empty<T>();
            }
            
            var json = await File.ReadAllTextAsync(path);
            var data = _serializer.Deserialize<IEnumerable<T>>(json);
            
            DataLoaded?.Invoke(this, new DataLoadedEventArgs { Path = path, Data = data });
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load collection from {Path}", path);
            throw;
        }
    }
    
    public async Task<bool> SaveAsync<T>(string path, T data) where T : class
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = _serializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
            
            DataSaved?.Invoke(this, new DataSavedEventArgs { Path = path, Data = data });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save data to {Path}", path);
            return false;
        }
    }
}

// HotReloadDataLoader.cs
public class HotReloadDataLoader : IDataLoader
{
    private readonly IDataLoader _innerLoader;
    private readonly Dictionary<string, FileSystemWatcher> _watchers;
    private readonly ILogger<HotReloadDataLoader> _logger;
    
    public HotReloadDataLoader(IDataLoader innerLoader, ILogger<HotReloadDataLoader> logger)
    {
        _innerLoader = innerLoader;
        _watchers = new Dictionary<string, FileSystemWatcher>();
        _logger = logger;
    }
    
    public async Task<T> LoadAsync<T>(string path) where T : class
    {
        SetupWatcher(path);
        return await _innerLoader.LoadAsync<T>(path);
    }
    
    public async Task<IEnumerable<T>> LoadCollectionAsync<T>(string path) where T : class
    {
        SetupWatcher(path);
        return await _innerLoader.LoadCollectionAsync<T>(path);
    }
    
    private void SetupWatcher(string path)
    {
        if (_watchers.ContainsKey(path)) return;
        
        var directory = Path.GetDirectoryName(path);
        var filename = Path.GetFileName(path);
        
        var watcher = new FileSystemWatcher(directory, filename);
        watcher.Changed += async (sender, e) =>
        {
            try
            {
                await Task.Delay(100); // Wait for file write to complete
                _logger.LogInformation("Data file changed: {Path}", path);
                
                // Trigger reload event
                DataLoaded?.Invoke(this, new DataLoadedEventArgs { Path = path });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle file change for {Path}", path);
            }
        };
        
        watcher.EnableRaisingEvents = true;
        _watchers[path] = watcher;
    }
    
    public event EventHandler<DataLoadedEventArgs> DataLoaded;
    public event EventHandler<DataSavedEventArgs> DataSaved;
}
```

### 4. Data Validation System (데이터 검증 시스템)

#### 4.1 Data Validators (데이터 검증기)
```csharp
// IDataValidator.cs
public interface IDataValidator
{
    ValidationResult Validate<T>(T data);
    ValidationResult ValidateCollection<T>(IEnumerable<T> data);
}

// BlockDataValidator.cs
public class BlockDataValidator : IDataValidator
{
    public ValidationResult Validate<T>(T data)
    {
        if (data is not BlockData blockData)
        {
            return new ValidationResult { IsValid = false, Errors = ["Data is not BlockData"] };
        }
        
        var errors = new List<string>();
        var warnings = new List<string>();
        
        // Validate required fields
        if (string.IsNullOrEmpty(blockData.Id))
            errors.Add("Block ID is required");
        
        if (string.IsNullOrEmpty(blockData.Name))
            errors.Add("Block name is required");
        
        // Validate properties
        if (blockData.Properties != null)
        {
            if (blockData.Properties.Hardness < 0)
                errors.Add("Block hardness cannot be negative");
            
            if (blockData.Properties.Resistance < 0)
                errors.Add("Block resistance cannot be negative");
            
            if (blockData.Properties.LightLevel < 0 || blockData.Properties.LightLevel > 15)
                errors.Add("Block light level must be between 0 and 15");
        }
        
        // Validate physics
        if (blockData.Physics != null)
        {
            if (blockData.Physics.Density < 0)
                errors.Add("Block density cannot be negative");
        }
        
        // Validate texture path
        if (!string.IsNullOrEmpty(blockData.Visual?.TexturePath))
        {
            if (!File.Exists($"Assets/{blockData.Visual.TexturePath}.png"))
                warnings.Add($"Texture file not found: {blockData.Visual.TexturePath}");
        }
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
    
    public ValidationResult ValidateCollection<T>(IEnumerable<T> data)
    {
        if (data is not IEnumerable<BlockData> blockDataList)
        {
            return new ValidationResult { IsValid = false, Errors = ["Data is not BlockData collection"] };
        }
        
        var allErrors = new List<string>();
        var allWarnings = new List<string>();
        var ids = new HashSet<string>();
        
        foreach (var blockData in blockDataList)
        {
            var result = Validate(blockData);
            allErrors.AddRange(result.Errors);
            allWarnings.AddRange(result.Warnings);
            
            // Check for duplicate IDs
            if (!string.IsNullOrEmpty(blockData.Id))
            {
                if (ids.Contains(blockData.Id))
                    allErrors.Add($"Duplicate block ID: {blockData.Id}");
                else
                    ids.Add(blockData.Id);
            }
        }
        
        return new ValidationResult
        {
            IsValid = allErrors.Count == 0,
            Errors = allErrors,
            Warnings = allWarnings
        };
    }
}
```

### 5. Data-Driven Game Logic (데이터 기반 게임 로직)

#### 5.1 Data-Driven Block System (데이터 기반 블록 시스템)
```csharp
// DataDrivenBlockManager.cs
public class DataDrivenBlockManager
{
    private readonly IDataRepository<BlockData> _blockRepository;
    private readonly IDataCache _cache;
    private readonly ILogger<DataDrivenBlockManager> _logger;
    private readonly Dictionary<string, BlockData> _blockDataCache;
    
    public DataDrivenBlockManager(
        IDataRepository<BlockData> blockRepository,
        IDataCache cache,
        ILogger<DataDrivenBlockManager> _logger)
    {
        _blockRepository = blockRepository;
        _cache = cache;
        _logger = logger;
        _blockDataCache = new Dictionary<string, BlockData>();
    }
    
    public async Task<BlockData> GetBlockDataAsync(string blockId)
    {
        if (_blockDataCache.TryGetValue(blockId, out var cachedData))
        {
            return cachedData;
        }
        
        var blockData = await _blockRepository.GetByIdAsync(blockId);
        if (blockData != null)
        {
            _blockDataCache[blockId] = blockData;
        }
        
        return blockData;
    }
    
    public async Task<bool> CanBreakBlockAsync(string blockId, string playerId, ToolData tool = null)
    {
        var blockData = await GetBlockDataAsync(blockId);
        if (blockData == null) return false;
        
        // Check if block can be broken
        if (!blockData.Interaction.CanInteract)
            return false;
        
        if (!blockData.Interaction.Interactions.Contains("break"))
            return false;
        
        // Check tool requirements
        if (blockData.Properties.RequiresTool)
        {
            if (tool == null) return false;
            
            if (tool.Type != blockData.Properties.RequiredTool)
                return false;
            
            if (tool.Level < blockData.Properties.RequiredToolLevel)
                return false;
        }
        
        return true;
    }
    
    public async Task<ItemStack[]> GetBlockDropsAsync(string blockId, ToolData tool = null)
    {
        var blockData = await GetBlockDataAsync(blockId);
        if (blockData?.Interaction?.DropTable == null)
        {
            return Array.Empty<ItemStack>();
        }
        
        var dropTable = await GetDropTableAsync(blockData.Interaction.DropTable);
        if (dropTable == null)
        {
            return Array.Empty<ItemStack>();
        }
        
        return CalculateDrops(dropTable, tool);
    }
    
    public async Task<int> GetBlockBreakExperienceAsync(string blockId)
    {
        var blockData = await GetBlockDataAsync(blockId);
        if (blockData?.Interaction?.Experience == null)
        {
            return 0;
        }
        
        var exp = blockData.Interaction.Experience;
        return Random.Shared.Next(exp.Min, exp.Max + 1);
    }
    
    private async Task<DropTableData> GetDropTableAsync(string dropTableId)
    {
        // Implementation for loading drop table data
        return null;
    }
    
    private ItemStack[] CalculateDrops(DropTableData dropTable, ToolData tool)
    {
        // Implementation for calculating drops based on drop table and tool
        return Array.Empty<ItemStack>();
    }
}
```

### 6. External Data Integration (외부 데이터 통합)

#### 6.1 API Data Provider (API 데이터 제공자)
```csharp
// IExternalDataProvider.cs
public interface IExternalDataProvider
{
    Task<T> GetDataAsync<T>(string endpoint, Dictionary<string, string> parameters = null);
    Task<bool> SendDataAsync<T>(string endpoint, T data);
    event EventHandler<ExternalDataEventArgs> DataReceived;
    event EventHandler<ExternalDataEventArgs> DataSent;
}

// RestApiDataProvider.cs
public class RestApiDataProvider : IExternalDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RestApiDataProvider> _logger;
    private readonly string _baseUrl;
    
    public RestApiDataProvider(HttpClient httpClient, ILogger<RestApiDataProvider> logger, string baseUrl)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = baseUrl;
    }
    
    public async Task<T> GetDataAsync<T>(string endpoint, Dictionary<string, string> parameters = null)
    {
        try
        {
            var url = $"{_baseUrl}/{endpoint}";
            if (parameters != null && parameters.Count > 0)
            {
                var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
                url += $"?{queryString}";
            }
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<T>(json);
            
            DataReceived?.Invoke(this, new ExternalDataEventArgs { Endpoint = endpoint, Data = data });
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data from {Endpoint}", endpoint);
            throw;
        }
    }
    
    public async Task<bool> SendDataAsync<T>(string endpoint, T data)
    {
        try
        {
            var url = $"{_baseUrl}/{endpoint}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            var success = response.IsSuccessStatusCode;
            
            if (success)
            {
                DataSent?.Invoke(this, new ExternalDataEventArgs { Endpoint = endpoint, Data = data });
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send data to {Endpoint}", endpoint);
            return false;
        }
    }
}
```

## Implementation Plan (구현 계획)

### Phase 1: Core Data Infrastructure (핵심 데이터 인프라)
1. **Data Repository Pattern** 구현
2. **Data Manager** 개발
3. **Data Loader** 시스템 구축
4. **Data Validation** 검증 시스템

### Phase 2: Game Data Systems (게임 데이터 시스템)
1. **Block Data System** 블록 데이터 시스템
2. **Item Data System** 아이템 데이터 시스템
3. **Recipe Data System** 레시피 데이터 시스템
4. **Drop Table System** 드롭 테이블 시스템

### Phase 3: Dynamic Loading (동적 로딩)
1. **Hot Reload** 핫 리로드 구현
2. **External Data** 외부 데이터 통합
3. **Cache System** 캐시 시스템
4. **Performance** 성능 최적화

### Phase 4: Integration (통합)
1. **Game Logic Integration** 게임 로직 통합
2. **UI Integration** UI 통합
3. **Testing** 테스트
4. **Documentation** 문서화

## Expected Benefits (기대 효과)

### Development Benefits (개발상 이점)
- **Faster Development**: 빠른 개발 속도
- **Easier Balancing**: 쉬운 밸런싱
- **Modular Design**: 모듈식 디자인
- **Better Testing**: 향상된 테스트

### Operational Benefits (운영상 이점)
- **Hot Updates**: 핫 업데이트 지원
- **Remote Configuration**: 원격 설정
- **Better Analytics**: 향상된 분석
- **Easier Debugging**: 쉬운 디버깅

### User Experience Benefits (사용자 경험 이점)
- **Dynamic Content**: 동적 콘텐츠
- **Personalization**: 개인화
- **Real-time Updates**: 실시간 업데이트
- **Consistent Experience**: 일관된 경험
