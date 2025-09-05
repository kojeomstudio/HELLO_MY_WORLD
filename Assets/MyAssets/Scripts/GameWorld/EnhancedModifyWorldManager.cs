using Mono.Data.Sqlite;
using MapGenLib;
using UnityEngine;
using System;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 강화된 월드 블록 수정/삭제 관리 클래스
/// 마인크래프트 스타일의 블록 파괴/생성, 드롭 시스템, 도구 시스템을 포함
/// </summary>
public class EnhancedModifyWorldManager : MonoBehaviour
{
    #region 구조체 정의
    
    /// <summary>
    /// 블록 파괴 진행 상태를 추적하는 구조체
    /// </summary>
    private struct BlockBreakProgress
    {
        public Vector3Int Position;
        public float Progress;      // 0.0 ~ 1.0
        public float BreakTime;     // 총 파괴 시간
        public float ElapsedTime;   // 경과 시간
        public byte OriginalBlockType;
        public bool IsActive;
    }

    /// <summary>
    /// 도구별 채굴 속도 정의
    /// </summary>
    public enum ToolType
    {
        Hand = 0,
        WoodPickaxe = 1,
        StonePickaxe = 2,
        IronPickaxe = 3,
        DiamondPickaxe = 4,
        WoodAxe = 5,
        StoneAxe = 6,
        IronAxe = 7,
        DiamondAxe = 8,
        WoodShovel = 9,
        StoneShovel = 10,
        IronShovel = 11,
        DiamondShovel = 12
    }

    /// <summary>
    /// 블록 경도 및 채굴 정보
    /// </summary>
    [System.Serializable]
    public struct BlockMiningInfo
    {
        public BlockTileType blockType;
        public float hardness;          // 기본 파괴 시간 (초)
        public ToolType[] preferredTools; // 효과적인 도구들
        public float toolMultiplier;    // 올바른 도구 사용시 속도 배율
        public bool requiresSpecificTool; // 특정 도구가 필요한지
    }

    /// <summary>
    /// 드롭 아이템 정보
    /// </summary>
    private struct DropItem
    {
        public byte itemType;
        public int quantity;
        public float probability;
    }

    #endregion

    #region 필드 선언

    private SubWorld SelectWorldInstance;
    private int chunkSize = 0;
    
    // 블록 파괴 시스템
    private BlockBreakProgress currentBreakProgress;
    private bool isBreakingBlock = false;
    
    // 도구 시스템
    [SerializeField] private ToolType currentTool = ToolType.Hand;
    [SerializeField] private BlockMiningInfo[] blockMiningData;
    
    // 파티클 효과
    [SerializeField] private GameObject blockBreakParticlePrefab;
    [SerializeField] private GameObject blockPlaceParticlePrefab;
    
    // 사운드 효과
    [SerializeField] private AudioClip[] blockBreakSounds;
    [SerializeField] private AudioClip[] blockPlaceSounds;
    private AudioSource audioSource;

    // 크랙 애니메이션을 위한 머티리얼
    [SerializeField] private Material[] crackTextures; // 0-9단계 크랙 텍스처

    #endregion

    #region 초기화

    public void Init()
    {
        var gameWorldConfig = WorldConfigFile.Instance.GetConfig();
        chunkSize = gameWorldConfig.ChunkSize;
        
        // 오디오 소스 초기화
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 기본 블록 채굴 정보 초기화
        InitializeBlockMiningData();
        
        // 현재 파괴 진행상태 초기화
        currentBreakProgress = new BlockBreakProgress
        {
            IsActive = false
        };
    }
    
    /// <summary>
    /// 블록별 채굴 정보를 초기화합니다
    /// </summary>
    private void InitializeBlockMiningData()
    {
        blockMiningData = new BlockMiningInfo[]
        {
            new BlockMiningInfo { blockType = BlockTileType.GRASS, hardness = 0.6f, preferredTools = new[] { ToolType.WoodShovel, ToolType.StoneShovel, ToolType.IronShovel, ToolType.DiamondShovel }, toolMultiplier = 5f, requiresSpecificTool = false },
            new BlockMiningInfo { blockType = BlockTileType.DIRT, hardness = 0.5f, preferredTools = new[] { ToolType.WoodShovel, ToolType.StoneShovel, ToolType.IronShovel, ToolType.DiamondShovel }, toolMultiplier = 5f, requiresSpecificTool = false },
            new BlockMiningInfo { blockType = BlockTileType.STONE, hardness = 1.5f, preferredTools = new[] { ToolType.WoodPickaxe, ToolType.StonePickaxe, ToolType.IronPickaxe, ToolType.DiamondPickaxe }, toolMultiplier = 4f, requiresSpecificTool = true },
            new BlockMiningInfo { blockType = BlockTileType.WOOD, hardness = 2.0f, preferredTools = new[] { ToolType.WoodAxe, ToolType.StoneAxe, ToolType.IronAxe, ToolType.DiamondAxe }, toolMultiplier = 4f, requiresSpecificTool = false },
            new BlockMiningInfo { blockType = BlockTileType.COBBLESTONE, hardness = 2.0f, preferredTools = new[] { ToolType.WoodPickaxe, ToolType.StonePickaxe, ToolType.IronPickaxe, ToolType.DiamondPickaxe }, toolMultiplier = 4f, requiresSpecificTool = true },
            new BlockMiningInfo { blockType = BlockTileType.IRON, hardness = 3.0f, preferredTools = new[] { ToolType.StonePickaxe, ToolType.IronPickaxe, ToolType.DiamondPickaxe }, toolMultiplier = 6f, requiresSpecificTool = true },
            new BlockMiningInfo { blockType = BlockTileType.DIAMOND, hardness = 5.0f, preferredTools = new[] { ToolType.IronPickaxe, ToolType.DiamondPickaxe }, toolMultiplier = 8f, requiresSpecificTool = true }
        };
    }

    #endregion

    #region Public 메서드

    /// <summary>
    /// 블록 파괴 시작 (마우스 클릭 시작)
    /// </summary>
    public void StartBreakingBlock(Ray ray, Vector3 clickWorldPos)
    {
        RaycastHit hitInfo;
        bool hit = Physics.Raycast(ray, out hitInfo);
        if (hit)
        {
            AChunk chunk = hitInfo.collider.gameObject.GetComponent<AChunk>();
            if (chunk != null)
            {
                SelectWorldInstance = chunk.SubWorldInstance;
                CollideInfo collideInfo = SelectWorldInstance.CustomOctreeInstance.Collide(ray);
                Block hitBlock = collideInfo.GetBlock();
                
                Vector3Int blockPos = new Vector3Int(hitBlock.WorldDataIndexX, hitBlock.WorldDataIndexY, hitBlock.WorldDataIndexZ);
                byte blockType = SelectWorldInstance.WorldBlockData[blockPos.x, blockPos.y, blockPos.z].CurrentType;
                
                if (blockType != (byte)BlockTileType.EMPTY)
                {
                    StartBlockBreak(blockPos, blockType);
                }
            }
        }
    }
    
    /// <summary>
    /// 블록 파괴 중단 (마우스 클릭 해제)
    /// </summary>
    public void StopBreakingBlock()
    {
        if (isBreakingBlock)
        {
            isBreakingBlock = false;
            currentBreakProgress.IsActive = false;
            
            // 파괴 진행 시각적 효과 제거
            RemoveBreakingVisualEffects();
        }
    }
    
    /// <summary>
    /// 블록 생성 (우클릭)
    /// </summary>
    public void AddBlockByInput(Ray ray, Vector3 clickWorldPos, byte blockType)
    {
        AddBlockAt(ray, clickWorldPos, blockType);
    }
    
    /// <summary>
    /// 현재 도구 설정
    /// </summary>
    public void SetCurrentTool(ToolType tool)
    {
        currentTool = tool;
    }

    /// <summary>
    /// 특정 서브월드의 블록 수정 (네트워크용)
    /// </summary>
    public void ModifySpecificSubWorld(string areaID, string subWorldID, int blockIndex_X, int blockIndex_Y, int blockIndex_Z, byte modifiedTileValue)
    {
        if(WorldAreaManager.Instance != null)
        {
            WorldAreaManager.Instance.WorldAreas.TryGetValue(areaID, out WorldArea area);
            if (area != null)
            {
                area.SubWorldStates.TryGetValue(subWorldID, out SubWorldState subWorldState);
                if (subWorldState != null)
                {
                    SubWorld subWorld = subWorldState.SubWorldInstance;
                    BlockTileInfo blockTileInfo = BlockTileDataFile.Instance.GetBlockTileInfo((BlockTileType)modifiedTileValue);
                    
                    subWorld.WorldBlockData[blockIndex_X, blockIndex_Y, blockIndex_Z].CurrentType = modifiedTileValue;
                    subWorld.WorldBlockData[blockIndex_X, blockIndex_Y, blockIndex_Z].Durability = blockTileInfo.Durability;
                    Vector3 centerPos = KojeomUtility.ConvertCustomToVector3(subWorld.WorldBlockData[blockIndex_X, blockIndex_Y, blockIndex_Z].GetCenterPosition());
                    
                    if ((BlockTileType)modifiedTileValue == BlockTileType.EMPTY)
                    {
                        subWorld.WorldBlockData[blockIndex_X, blockIndex_Y, blockIndex_Z].bRendered = false;
                        CollideInfo col = subWorld.CustomOctreeInstance.Collide(centerPos);
                        if (col.bCollide == true)
                        {
                            subWorld.CustomOctreeInstance.Delete(col.CollisionPoint);
                        }
                    }
                    else
                    {
                        subWorld.WorldBlockData[blockIndex_X, blockIndex_Y, blockIndex_Z].bRendered = true;
                        CollideInfo col = subWorld.CustomOctreeInstance.Collide(centerPos);
                        if (col.bCollide == false)
                        {
                            subWorld.CustomOctreeInstance.Add(col.CollisionPoint);
                        }
                    }
                    UpdateChunkAt(blockIndex_X, blockIndex_Y, blockIndex_Z, modifiedTileValue, subWorld.ChunkSlots);
                }
            }
        }
    }

    #endregion

    #region Private 메서드 - 블록 파괴

    /// <summary>
    /// 블록 파괴 시작
    /// </summary>
    private void StartBlockBreak(Vector3Int blockPos, byte blockType)
    {
        // 기존 파괴 진행 중이면 중단
        if (isBreakingBlock)
        {
            StopBreakingBlock();
        }
        
        float breakTime = GetBlockBreakTime(blockType, currentTool);
        
        // 파괴할 수 없는 블록인 경우
        if (breakTime == float.MaxValue)
        {
            Debug.LogWarning($"블록 {(BlockTileType)blockType}는 현재 도구 {currentTool}로 파괴할 수 없습니다.");
            return;
        }
        
        currentBreakProgress = new BlockBreakProgress
        {
            Position = blockPos,
            Progress = 0f,
            BreakTime = breakTime,
            ElapsedTime = 0f,
            OriginalBlockType = blockType,
            IsActive = true
        };
        
        isBreakingBlock = true;
        
        // 파괴 시작 사운드 재생
        PlayBlockSound(blockType, true);
        
        // 즉시 파괴되는 경우 (예: 잔디를 손으로)
        if (breakTime <= 0.1f)
        {
            CompleteBlockBreak();
        }
    }
    
    /// <summary>
    /// 블록 파괴 완료
    /// </summary>
    private void CompleteBlockBreak()
    {
        if (!isBreakingBlock) return;
        
        Vector3Int pos = currentBreakProgress.Position;
        byte blockType = currentBreakProgress.OriginalBlockType;
        
        // 블록 제거
        SetBlockForDelete(pos.x, pos.y, pos.z, (byte)BlockTileType.EMPTY);
        
        // 아이템 드롭
        DropBlockItems(pos, blockType);
        
        // 파괴 완료 사운드 및 이펙트
        PlayBlockSound(blockType, false);
        SpawnBlockBreakParticles(pos, blockType);
        
        // 파괴 상태 초기화
        isBreakingBlock = false;
        currentBreakProgress.IsActive = false;
        
        // 시각적 효과 제거
        RemoveBreakingVisualEffects();
    }
    
    /// <summary>
    /// 블록 파괴 시간 계산
    /// </summary>
    private float GetBlockBreakTime(byte blockType, ToolType tool)
    {
        BlockTileType tileType = (BlockTileType)blockType;
        
        foreach (var miningInfo in blockMiningData)
        {
            if (miningInfo.blockType == tileType)
            {
                float baseTime = miningInfo.hardness;
                
                // 올바른 도구 사용 시 속도 향상
                if (IsToolEffective(tool, miningInfo.preferredTools))
                {
                    baseTime /= GetToolEfficiency(tool, miningInfo.toolMultiplier);
                }
                
                // 특정 도구가 필요하지만 잘못된 도구 사용 시
                if (miningInfo.requiresSpecificTool && !IsToolEffective(tool, miningInfo.preferredTools))
                {
                    return float.MaxValue; // 파괴 불가
                }
                
                return baseTime;
            }
        }
        
        // 기본값
        return 1.0f;
    }
    
    /// <summary>
    /// 도구가 블록에 효과적인지 확인
    /// </summary>
    private bool IsToolEffective(ToolType tool, ToolType[] effectiveTools)
    {
        foreach (var effectiveTool in effectiveTools)
        {
            if (tool == effectiveTool) return true;
        }
        return false;
    }
    
    /// <summary>
    /// 도구의 효율성 계산
    /// </summary>
    private float GetToolEfficiency(ToolType tool, float baseMultiplier)
    {
        return tool switch
        {
            ToolType.Hand => 1f,
            ToolType.WoodPickaxe or ToolType.WoodAxe or ToolType.WoodShovel => baseMultiplier * 2f,
            ToolType.StonePickaxe or ToolType.StoneAxe or ToolType.StoneShovel => baseMultiplier * 4f,
            ToolType.IronPickaxe or ToolType.IronAxe or ToolType.IronShovel => baseMultiplier * 6f,
            ToolType.DiamondPickaxe or ToolType.DiamondAxe or ToolType.DiamondShovel => baseMultiplier * 8f,
            _ => 1f
        };
    }

    #endregion

    #region Private 메서드 - 드롭 시스템

    /// <summary>
    /// 블록 드롭 아이템 생성
    /// </summary>
    private void DropBlockItems(Vector3Int blockPos, byte blockType)
    {
        BlockTileType tileType = (BlockTileType)blockType;
        List<DropItem> drops = GetBlockDrops(tileType, currentTool);
        
        Vector3 dropPosition = new Vector3(blockPos.x, blockPos.y, blockPos.z) + Vector3.one * 0.5f;
        
        foreach (var drop in drops)
        {
            if (UnityEngine.Random.value <= drop.probability)
            {
                for (int i = 0; i < drop.quantity; i++)
                {
                    // 드롭 아이템 생성 로직 (기존 데이터베이스 연동)
                    CreateDroppedItem(drop.itemType, dropPosition + UnityEngine.Random.insideUnitSphere * 0.3f);
                }
            }
        }
    }
    
    /// <summary>
    /// 블록별 드롭 아이템 계산
    /// </summary>
    private List<DropItem> GetBlockDrops(BlockTileType blockType, ToolType tool)
    {
        List<DropItem> drops = new List<DropItem>();
        
        switch (blockType)
        {
            case BlockTileType.GRASS:
                drops.Add(new DropItem { itemType = (byte)BlockTileType.DIRT, quantity = 1, probability = 1.0f });
                if (UnityEngine.Random.value < 0.125f) // 12.5% 확률로 씨앗 드롭
                {
                    drops.Add(new DropItem { itemType = 100, quantity = 1, probability = 1.0f }); // 씨앗 ID
                }
                break;
                
            case BlockTileType.STONE:
                if (IsToolEffective(tool, new[] { ToolType.WoodPickaxe, ToolType.StonePickaxe, ToolType.IronPickaxe, ToolType.DiamondPickaxe }))
                {
                    drops.Add(new DropItem { itemType = (byte)BlockTileType.COBBLESTONE, quantity = 1, probability = 1.0f });
                }
                break;
                
            case BlockTileType.IRON:
                if (IsToolEffective(tool, new[] { ToolType.StonePickaxe, ToolType.IronPickaxe, ToolType.DiamondPickaxe }))
                {
                    drops.Add(new DropItem { itemType = (byte)BlockTileType.IRON, quantity = 1, probability = 1.0f });
                }
                break;
                
            case BlockTileType.DIAMOND:
                if (IsToolEffective(tool, new[] { ToolType.IronPickaxe, ToolType.DiamondPickaxe }))
                {
                    drops.Add(new DropItem { itemType = 101, quantity = 1, probability = 1.0f }); // 다이아몬드 원석 ID
                }
                break;
                
            case BlockTileType.WOOD:
                // 나무는 어떤 도구로든 드롭
                drops.Add(new DropItem { itemType = (byte)blockType, quantity = 1, probability = 1.0f });
                break;
                
            default:
                // 기본적으로 자기 자신을 드롭
                drops.Add(new DropItem { itemType = (byte)blockType, quantity = 1, probability = 1.0f });
                break;
        }
        
        return drops;
    }
    
    /// <summary>
    /// 드롭된 아이템 생성
    /// </summary>
    private void CreateDroppedItem(byte itemType, Vector3 position)
    {
        // 기존 데이터베이스 시스템과 연동하여 인벤토리에 아이템 추가
        UpdateUserItem(itemType);
        
        // TODO: 실제 3D 아이템 오브젝트 생성 (선택사항)
        // 지금은 직접 인벤토리에 추가
    }

    #endregion

    #region Private 메서드 - 블록 생성

    private void AddBlockAt(Ray ray, Vector3 clickWorldPos, byte blockType)
    {
        RaycastHit hitInfo;
        bool hit = Physics.Raycast(ray, out hitInfo);
        if (hit)
        {
            Vector3 offset = Vector3.zero; 

            AChunk chunk = hitInfo.collider.gameObject.GetComponent<AChunk>();
            SelectWorldInstance = chunk.SubWorldInstance;
            CollideInfo collideInfo = SelectWorldInstance.CustomOctreeInstance.Collide(ray);

            Block hitBlock = collideInfo.GetBlock();
            int blockX = hitBlock.WorldDataIndexX;
            int blockY = hitBlock.WorldDataIndexY;
            int blockZ = hitBlock.WorldDataIndexZ;
            
            // 생성할 위치 계산 (인접한 면에 배치)
            offset = hitInfo.normal;
            blockX += (int)offset.x;
            blockY += (int)offset.y;
            blockZ += (int)offset.z;

            // 블록 생성 처리
            ProcessBlockCreate(blockX, blockY, blockZ, blockType);
        }
    }

    private void ProcessBlockCreate(int blockX, int blockY, int blockZ, byte blockType)
    {
        bool bValidX = SelectWorldInstance.WorldBlockData.GetLength(0) > blockX && 0 <= blockX;
        bool bValidY = SelectWorldInstance.WorldBlockData.GetLength(1) > blockY && 0 <= blockY;
        bool bValidZ = SelectWorldInstance.WorldBlockData.GetLength(2) > blockZ && 0 <= blockZ;
        
        if (bValidX && bValidY && bValidZ)
        {
            Vector3 blockPosition = new Vector3(blockX, blockY, blockZ);
            SelectWorldInstance.CustomOctreeInstance.Add(blockPosition);
            SetBlockForAdd(blockX, blockY, blockZ, blockType);
            SelectWorldInstance.WorldBlockData[blockX, blockY, blockZ].bRendered = true;
            
            // 블록 배치 사운드 및 파티클 효과
            PlayBlockSound(blockType, false);
            SpawnBlockPlaceParticles(new Vector3Int(blockX, blockY, blockZ), blockType);
        }
        else
        {
            KojeomLogger.DebugLog("ProcessBlockCreate() -> InValid Block Index X or Y or Z", LOG_TYPE.ERROR);
        }
    }

    #endregion

    #region Private 메서드 - 효과

    /// <summary>
    /// 블록 파괴 파티클 생성
    /// </summary>
    private void SpawnBlockBreakParticles(Vector3Int blockPos, byte blockType)
    {
        Vector3 position = new Vector3(blockPos.x, blockPos.y, blockPos.z) + Vector3.one * 0.5f;
        
        // 기존 파티클 시스템 사용
        ParticleEffectSpawnParams spawnParams;
        spawnParams.ParticleType = GetParticleTypeForBlock((BlockTileType)blockType);
        spawnParams.SpawnLocation = position;
        spawnParams.SpawnRotation = Quaternion.identity;
        spawnParams.bLooping = false;
        spawnParams.bStart = true;
        
        if (GameParticleEffectManager.Instance != null)
        {
            GameParticleEffectManager.Instance.SpawnParticleEffect(spawnParams);
        }
        
        // 추가 파티클 효과 (블록 파편)
        for (int i = 0; i < 8; i++)
        {
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 0.5f;
            ParticleEffectSpawnParams fragmentParams;
            fragmentParams.ParticleType = GameParticleType.BlockFragment;
            fragmentParams.SpawnLocation = position + randomOffset;
            fragmentParams.SpawnRotation = UnityEngine.Random.rotation;
            fragmentParams.bLooping = false;
            fragmentParams.bStart = true;
            
            if (GameParticleEffectManager.Instance != null)
            {
                GameParticleEffectManager.Instance.SpawnParticleEffect(fragmentParams);
            }
        }
    }

    /// <summary>
    /// 블록 배치 파티클 생성
    /// </summary>
    private void SpawnBlockPlaceParticles(Vector3Int blockPos, byte blockType)
    {
        Vector3 position = new Vector3(blockPos.x, blockPos.y, blockPos.z) + Vector3.one * 0.5f;
        
        ParticleEffectSpawnParams spawnParams;
        spawnParams.ParticleType = GameParticleType.BlockPlace;
        spawnParams.SpawnLocation = position;
        spawnParams.SpawnRotation = Quaternion.identity;
        spawnParams.bLooping = false;
        spawnParams.bStart = true;
        
        if (GameParticleEffectManager.Instance != null)
        {
            GameParticleEffectManager.Instance.SpawnParticleEffect(spawnParams);
        }
    }
    
    /// <summary>
    /// 블록 타입에 따른 파티클 타입 반환
    /// </summary>
    private GameParticleType GetParticleTypeForBlock(BlockTileType blockType)
    {
        return blockType switch
        {
            BlockTileType.STONE => GameParticleType.StoneBreak,
            BlockTileType.WOOD => GameParticleType.WoodBreak,
            BlockTileType.GRASS => GameParticleType.GrassBreak,
            BlockTileType.DIRT => GameParticleType.DirtBreak,
            _ => GameParticleType.FireworksGreenSmall
        };
    }
    
    /// <summary>
    /// 블록 사운드 재생
    /// </summary>
    private void PlayBlockSound(byte blockType, bool isBreakStart)
    {
        if (audioSource == null) return;
        
        AudioClip[] soundArray = isBreakStart ? blockBreakSounds : blockPlaceSounds;
        if (soundArray != null && soundArray.Length > 0)
        {
            int soundIndex = GetSoundIndexForBlock((BlockTileType)blockType);
            if (soundIndex < soundArray.Length && soundArray[soundIndex] != null)
            {
                audioSource.PlayOneShot(soundArray[soundIndex]);
            }
        }
    }
    
    /// <summary>
    /// 블록 타입에 따른 사운드 인덱스 반환
    /// </summary>
    private int GetSoundIndexForBlock(BlockTileType blockType)
    {
        return blockType switch
        {
            BlockTileType.STONE => 0,
            BlockTileType.WOOD => 1,
            BlockTileType.GRASS => 2,
            BlockTileType.DIRT => 2,
            _ => 0
        };
    }
    
    /// <summary>
    /// 파괴 진행 시각적 효과 제거
    /// </summary>
    private void RemoveBreakingVisualEffects()
    {
        // TODO: 블록 크랙 텍스처 제거 등
    }

    /// <summary>
    /// 파괴 진행 시각적 효과 업데이트
    /// </summary>
    private void UpdateBreakingVisualEffects()
    {
        // TODO: 블록에 크랙 텍스처 적용
        // 진행도에 따라 0~9단계의 크랙 텍스처 표시
        int crackStage = Mathf.FloorToInt(currentBreakProgress.Progress * 10f);
        crackStage = Mathf.Clamp(crackStage, 0, 9);
        
        // 크랙 텍스처 적용 로직 구현 필요
    }

    #endregion

    #region 기존 코드 호환성

    /// <summary>
    /// 사용자 아이템 업데이트 (기존 코드와 호환성 유지)
    /// </summary>
    private void UpdateUserItem(byte blockType)
    {
        StringBuilder conn = new StringBuilder();
        conn.AppendFormat(GameDBManager.GetInstance().GetDBConnectionPath(), Application.dataPath);

        IDbConnection dbconn;
        IDbCommand dbcmd;
        using (dbconn = (IDbConnection)new SqliteConnection(conn.ToString()))
        {
            using (dbcmd = dbconn.CreateCommand())
            {
                string itemID = blockType.ToString();
                ItemInfo itemInfo = ItemTableReader.GetInstance().GetItemInfo(itemID);
                string type = itemInfo.Type.ToString();
                try
                {
                    dbconn.Open();
                    StringBuilder sqlQuery = new StringBuilder();
                    sqlQuery.AppendFormat("INSERT INTO USER_ITEM (name, type, amount, id) VALUES('{0}', '{1}', 1, {2} )",
                        itemInfo.Name, type, itemID);
                    dbcmd.CommandText = sqlQuery.ToString();
                    dbcmd.ExecuteNonQuery();

                    dbconn.Close();
                }
                catch (SqliteException e)
                {
                    if (SQLiteErrorCode.Constraint == e.ErrorCode)
                    {
                        StringBuilder sqlQuery = new StringBuilder();
                        sqlQuery.AppendFormat("SELECT amount FROM USER_ITEM WHERE id = '{0}'", itemID);
                        dbcmd.CommandText = sqlQuery.ToString();
                        IDataReader reader = dbcmd.ExecuteReader();
                        reader.Read();
                        int itemAmount = reader.GetInt32(0);
                        itemAmount++;
                        reader.Close();

                        sqlQuery.Remove(0, sqlQuery.Length);
                        sqlQuery.AppendFormat("UPDATE USER_ITEM SET amount = '{0}' WHERE id = '{1}'",
                            itemAmount, itemID);
                        dbcmd.CommandText = sqlQuery.ToString();
                        dbcmd.ExecuteNonQuery();

                        dbconn.Close();
                    }
                }
            }
            dbconn.Close();
        }
    }

    private void SetBlockForAdd(int x, int y, int z, byte block)
    {
        var gameWorldConfig = WorldConfigFile.Instance.GetConfig();
        if ((x < gameWorldConfig.SubWorldSizeX) &&
           (y < gameWorldConfig.SubWorldSizeY) &&
           (z < gameWorldConfig.SubWorldSizeZ) &&
           (x >= 0) && (y >= 0) && (z >= 0)) 
        {
            BlockTileInfo tileInfo = BlockTileDataFile.Instance.GetBlockTileInfo((BlockTileType)block);
            SelectWorldInstance.WorldBlockData[x, y, z].CurrentType = block;
            SelectWorldInstance.WorldBlockData[x, y, z].Durability = tileInfo.Durability;
            SelectWorldInstance.WorldBlockData[x, y, z].bRendered = true;
            UpdateChunkAt(x, y, z, block);
        }
        else
        {
            GameMessage.SetMessage("허용 범위를 벗어나 블록 생성이 불가능합니다.", GameMessage.MESSAGE_TYPE.CANT_CREATE_BLOCK);
            UIPopupSupervisor.OpenPopupUI(UI_POPUP_TYPE.GameMessage);
        }
    }

    private void SetBlockForDelete(int x, int y, int z, byte block)
    {
        var gameWorldConfig = WorldConfigFile.Instance.GetConfig();
        if ((x < gameWorldConfig.SubWorldSizeX) &&
           (y < gameWorldConfig.SubWorldSizeY) &&
           (z < gameWorldConfig.SubWorldSizeZ) &&
           (x >= 0) && (y >= 0) && (z >= 0))
        {
            BlockTileInfo tileInfo = BlockTileDataFile.Instance.GetBlockTileInfo((BlockTileType)block);
            SelectWorldInstance.WorldBlockData[x, y, z].CurrentType = block;
            SelectWorldInstance.WorldBlockData[x, y, z].Durability = tileInfo.Durability;
            SelectWorldInstance.WorldBlockData[x, y, z].bRendered = false;
            UpdateChunkAt(x, y, z, block);
        }
        else
        {
            GameMessage.SetMessage("허용 범위를 벗어나 블록 생성이 불가능합니다.", GameMessage.MESSAGE_TYPE.CANT_CREATE_BLOCK);
            UIPopupSupervisor.OpenPopupUI(UI_POPUP_TYPE.GameMessage);
        }
    }

    private void UpdateChunkAt(int x, int y, int z, byte block)
    {
        var gameWorldConfig = WorldConfigFile.Instance.GetConfig();
        int updateX, updateY, updateZ;
        updateX = Mathf.FloorToInt(x / chunkSize);
        updateY = Mathf.FloorToInt(y / chunkSize);
        updateZ = Mathf.FloorToInt(z / chunkSize);
        
        if (SelectWorldInstance.ChunkSlots[updateX, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN] == null)
        {
            return;
        }
        
        SelectWorldInstance.ChunkSlots[updateX, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;

        // 인접 청크 업데이트
        if (x - (chunkSize * updateX) == 0 && updateX != 0)
        {
            SelectWorldInstance.ChunkSlots[updateX - 1, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }

        if (x - (chunkSize * updateX) == gameWorldConfig.ChunkSize && updateX != SelectWorldInstance.ChunkSlots.GetLength(0) - 1)
        {
            SelectWorldInstance.ChunkSlots[updateX + 1, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }

        if (y - (chunkSize * updateY) == 0 && updateY != 0)
        {
            SelectWorldInstance.ChunkSlots[updateX, updateY - 1, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }

        if (y - (chunkSize * updateY) == gameWorldConfig.ChunkSize && updateY != SelectWorldInstance.ChunkSlots.GetLength(1) - 1)
        {
            SelectWorldInstance.ChunkSlots[updateX, updateY + 1, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }

        if (z - (chunkSize * updateZ) == 0 && updateZ != 0)
        {
            SelectWorldInstance.ChunkSlots[updateX, updateY, updateZ - 1].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }

        if (z - (chunkSize * updateZ) == gameWorldConfig.ChunkSize && updateZ != SelectWorldInstance.ChunkSlots.GetLength(2) - 1)
        {
            SelectWorldInstance.ChunkSlots[updateX, updateY, updateZ + 1].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }
    }

    private void UpdateChunkAt(int x, int y, int z, byte block, ChunkSlot[,,] chunkSlots)
    {
        var gameWorldConfig = WorldConfigFile.Instance.GetConfig();
        int updateX, updateY, updateZ;
        updateX = Mathf.FloorToInt(x / chunkSize);
        updateY = Mathf.FloorToInt(y / chunkSize);
        updateZ = Mathf.FloorToInt(z / chunkSize);
        
        if (chunkSlots[updateX, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN] == null)
        {
            return;
        }
        
        chunkSlots[updateX, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;

        // 인접 청크 업데이트 로직...
    }

    #endregion

    #region Unity 생명주기

    void Update()
    {
        // 블록 파괴 진행 처리
        if (isBreakingBlock && currentBreakProgress.IsActive)
        {
            currentBreakProgress.ElapsedTime += Time.deltaTime;
            currentBreakProgress.Progress = Mathf.Clamp01(currentBreakProgress.ElapsedTime / currentBreakProgress.BreakTime);
            
            // 파괴 완료 체크
            if (currentBreakProgress.Progress >= 1.0f)
            {
                CompleteBlockBreak();
            }
            else
            {
                // 파괴 진행 시각적 효과 업데이트
                UpdateBreakingVisualEffects();
            }
        }
    }

    #endregion
}