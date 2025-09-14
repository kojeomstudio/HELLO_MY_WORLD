using Mono.Data.Sqlite;
using MapGenLib;
using UnityEngine;
using System;
using System.Text;
using System.Data;

/// <summary>
/// 게임내 사용자가 월드 블록을 수정/삭제를 관리하는 클래스.
/// 마인크래프트와 같은 블록 조작 기능을 제공합니다.
/// </summary>
public class ModifyWorldManager : MonoBehaviour
{
    /// <summary>
    /// 블록 처리를 위한 내부 데이터 구조체
    /// </summary>
    private struct ProcessBlockData_Internal
    {
        public CollideInfo CollideInfo;
        public Vector3 UpdatePosition;
        public bool bCreate;
        public int BlockX;
        public int BlockY;
        public int BlockZ;
        public byte BlockType;
        public float PlaceDistance; // 블록 설치 거리 추가
        public bool IsValidPlacement; // 유효한 설치 위치인지 확인
    }

    private SubWorld SelectWorldInstance;
    private int chunkSize = 0;
    
    // 블록 설치/파괴 관련 설정
    private const float MAX_BLOCK_REACH_DISTANCE = 6.0f; // 최대 블록 조작 거리
    private const float MIN_BLOCK_PLACE_DISTANCE = 1.0f; // 최소 블록 설치 거리
    
    /// <summary>
    /// 매니저 초기화
    /// </summary>
    public void Init()
    {
        var gameWorldConfig = WorldConfigFile.Instance.GetConfig();
        chunkSize = gameWorldConfig.ChunkSize;
    }

    public void ModifySpecificSubWorld(string areaID, string subWorldID, int blockIndex_X, int blockIndex_Y, int blockIndex_Z, byte modifiedTileValue)
    {
        if(WorldAreaManager.Instance != null)
        {
            WorldAreaManager.Instance.WorldAreas.TryGetValue(areaID, out WorldArea area);
            area.SubWorldStates.TryGetValue(subWorldID, out SubWorldState subWorldState);
            //
            SubWorld subWorld = subWorldState.SubWorldInstance;
            BlockTileInfo blockTileInfo = BlockTileDataFile.Instance.GetBlockTileInfo((BlockTileType)modifiedTileValue);
            //
            subWorld.WorldBlockData[blockIndex_X, blockIndex_Y, blockIndex_Z].CurrentType = modifiedTileValue;
            subWorld.WorldBlockData[blockIndex_X, blockIndex_Y, blockIndex_Z].Durability = blockTileInfo.Durability;
            Vector3 centerPos = KojeomUtility.ConvertCustomToVector3(subWorld.WorldBlockData[blockIndex_X, blockIndex_Y, blockIndex_Z].GetCenterPosition());
            if ((BlockTileType)modifiedTileValue == BlockTileType.EMPTY)
            {
                subWorld.WorldBlockData[blockIndex_X, blockIndex_Y, blockIndex_Z].bRendered = false;
                // 지워진 블록에 옥트리 노드가 남아있다면 삭제.
                CollideInfo col = subWorld.CustomOctreeInstance.Collide(centerPos);
                if (col.bCollide == true)
                {
                    subWorld.CustomOctreeInstance.Delete(col.CollisionPoint);
                }
            }
            else
            {
                subWorld.WorldBlockData[blockIndex_X, blockIndex_Y, blockIndex_Z].bRendered = true;
                // 블록 타입이 변경된 지점에 옥트리 노드가 없다면 새로 생성.
                CollideInfo col = subWorld.CustomOctreeInstance.Collide(centerPos);
                if (col.bCollide == false)
                {
                    subWorld.CustomOctreeInstance.Add(col.CollisionPoint);
                }
            }
            UpdateChunkAt(blockIndex_X, blockIndex_Y, blockIndex_Z, modifiedTileValue, subWorld.ChunkSlots);
        }
    }

    /// <summary>
    /// 입력에 의한 블록 삭제 (개선된 거리 체크 포함)
    /// </summary>
    public void DeleteBlockByInput(Ray ray, Vector3 clickWorldPos, byte blockType)
    {
        if (IsValidInteractionDistance(clickWorldPos))
        {
            DeleteBlockAt(ray, clickWorldPos, blockType);
        }
        else
        {
            GameMessage.SetMessage("너무 멀어서 블록을 부술 수 없습니다.", GameMessage.MESSAGE_TYPE.CANT_CREATE_BLOCK);
            UIPopupSupervisor.OpenPopupUI(UI_POPUP_TYPE.GameMessage);
        }
    }

    /// <summary>
    /// 입력에 의한 블록 추가 (개선된 거리 체크 포함)
    /// </summary>
    public void AddBlockByInput(Ray ray, Vector3 clickWorldPos, byte blockType)
    {
        if (IsValidInteractionDistance(clickWorldPos))
        {
            AddBlockAt(ray, clickWorldPos, blockType);
        }
        else
        {
            GameMessage.SetMessage("너무 멀어서 블록을 설치할 수 없습니다.", GameMessage.MESSAGE_TYPE.CANT_CREATE_BLOCK);
            UIPopupSupervisor.OpenPopupUI(UI_POPUP_TYPE.GameMessage);
        }
    }
    
    /// <summary>
    /// 플레이어와 대상 위치 간의 거리가 유효한지 확인
    /// </summary>
    private bool IsValidInteractionDistance(Vector3 targetPosition)
    {
        if (GamePlayerManager.Instance?.GetCurrentPlayer()?.transform == null)
            return false;
            
        Vector3 playerPosition = GamePlayerManager.Instance.GetCurrentPlayer().transform.position;
        float distance = Vector3.Distance(playerPosition, targetPosition);
        return distance <= MAX_BLOCK_REACH_DISTANCE;
    }

    private void DeleteBlockAt(Ray ray, Vector3 clickWorldPos, byte blockType)
    {
        RayCastingProcess(ray, blockType, false);
    }
    private void AddBlockAt(Ray ray, Vector3 clickWorldPos, byte blockType)
    {
        RayCastingProcess(ray, blockType, true);
    }
    private void SelectWorld(Vector3 clickWorldPos)
    {
        foreach (var element in WorldAreaManager.Instance.ContainedWorldArea(clickWorldPos).SubWorldStates)
        {
            if (CustomAABB.IsInterSectPoint(element.Value.SubWorldInstance.CustomOctreeInstance.RootMinBound,
                                            element.Value.SubWorldInstance.CustomOctreeInstance.RootMaxBound, clickWorldPos))
            {
                SelectWorldInstance = element.Value.SubWorldInstance;
                break;
            }
        }
    }

    private void RayCastingProcess(Ray ray, byte blockType, bool bCreate)
    {
        RaycastHit hitInfo;
        bool hit = Physics.Raycast(ray, out hitInfo);
        if (hit == true)
        {
            Vector3 offset = Vector3.zero; 

            AChunk chunk = hitInfo.collider.gameObject.GetComponent<AChunk>();
            SelectWorldInstance = chunk.SubWorldInstance;
            CollideInfo collideInfo = SelectWorldInstance.CustomOctreeInstance.Collide(ray);

            Block hitBlock = collideInfo.GetBlock();
            int blockX = hitBlock.WorldDataIndexX;
            int blockY = hitBlock.WorldDataIndexY;
            int blockZ = hitBlock.WorldDataIndexZ;
            if (bCreate == true)
            {
                offset = hitInfo.normal;
                blockX += (int)offset.x;
                blockY += (int)offset.y;
                blockZ += (int)offset.z;
                
                // 블록 설치 위치 유효성 검사
                if (!IsValidBlockPlacement(blockX, blockY, blockZ, SelectWorldInstance))
                {
                    GameMessage.SetMessage("이 위치에는 블록을 설치할 수 없습니다.", GameMessage.MESSAGE_TYPE.CANT_CREATE_BLOCK);
                    UIPopupSupervisor.OpenPopupUI(UI_POPUP_TYPE.GameMessage);
                    return;
                }
            }

            ProcessBlockData_Internal processData = new ProcessBlockData_Internal();
            processData.CollideInfo = collideInfo;
            processData.bCreate = bCreate;
            processData.BlockX = blockX;
            processData.BlockY = blockY;
            processData.BlockZ = blockZ;
            processData.BlockType = blockType;
            processData.UpdatePosition = collideInfo.HitBlockCenter + offset;
            processData.PlaceDistance = Vector3.Distance(ray.origin, processData.UpdatePosition);
            processData.IsValidPlacement = IsValidBlockPlacement(blockX, blockY, blockZ, SelectWorldInstance);
            if (GameStatusManager.CurrentGameModeState == GameModeState.SINGLE)
            {
                ProcessBlockCreateOrDelete(processData);
            }
            else if (GameStatusManager.CurrentGameModeState == GameModeState.MULTI)
            {
                // 블록 변경 패킷.
                SubWorldBlockPacketData packetData;
                packetData.AreaID = SelectWorldInstance.GetWorldAreaUniqueID();
                packetData.SubWorldID = SelectWorldInstance.UniqueID;
                packetData.BlockTypeValue = blockType;

                packetData.BlockIndex_X = blockX;
                packetData.BlockIndex_Y = blockY;
                packetData.BlockIndex_Z = blockZ;
                packetData.OwnerChunkType = (byte)SelectWorldInstance.WorldBlockData[blockX, blockY, blockZ].OwnerChunkType;
                // 패킷 전송.
                GameNetworkManager.GetInstance().RequestChangeSubWorldBlock(packetData, () =>
                {
                    ProcessBlockCreateOrDelete(processData);
                });
            }
        }
    }

    private Vector3 CalcBlockCreateOffset(Block block, Ray ray)
    {
        foreach(var group in block.PlaneGroup)
        {
            PlaneType type = group.Key;
            PlaneData data = group.Value;
            Vector3 pointOnPlane = new Vector3(data.Points[0].x, data.Points[0].y, data.Points[0].z);
            Vector3 planeNormal = new Vector3(data.SurfaceNormal.x, data.SurfaceNormal.y, data.SurfaceNormal.z);
            bool bIntersect = KojeomUtility.IntersectRayWithPlane(ray, pointOnPlane, planeNormal);
            if(bIntersect == true)
            {
                KojeomLogger.DebugLog(string.Format("Collided Plane Type : {0}, Normal : {1}", type, planeNormal));
                return planeNormal;
            }
        }
        return Vector3.zero;
    }

    private void ProcessBlockCreateOrDelete(ProcessBlockData_Internal processData)
    {
        bool bValidX = SelectWorldInstance.WorldBlockData.GetLength(0) > processData.BlockX && 0 <= processData.BlockX;
        bool bValidY = SelectWorldInstance.WorldBlockData.GetLength(1) > processData.BlockY && 0 <= processData.BlockY;
        bool bValidZ = SelectWorldInstance.WorldBlockData.GetLength(2) > processData.BlockZ && 0 <= processData.BlockZ;
        if (bValidX == false || bValidY == false || bValidZ == false)
        {
            KojeomLogger.DebugLog("ProcessBlockCreateOrDelete() -> InValid Block Index X or Y or Z", LOG_TYPE.ERROR);
            return;
        }

        if (processData.bCreate == true)
        {
            // 블록 생성 처리 - 개선된 로직
            if (processData.IsValidPlacement && processData.PlaceDistance <= MAX_BLOCK_REACH_DISTANCE)
            {
                SelectWorldInstance.CustomOctreeInstance.Add(processData.UpdatePosition);
                SetBlockForAdd(processData.BlockX, processData.BlockY, processData.BlockZ, processData.BlockType);
                SelectWorldInstance.WorldBlockData[processData.BlockX, processData.BlockY, processData.BlockZ].bRendered = true;
                
                // 블록 설치 사운드 재생
                PlayBlockPlaceSound(processData.BlockType, processData.UpdatePosition);
            }
            else
            {
                KojeomLogger.DebugLog("Invalid block placement attempt", LOG_TYPE.WARNING);
            }
        }
        else
        {
            // 블록 파괴 처리 - 개선된 파티클 효과
            SpawnBlockBreakParticles(processData.BlockType, processData.CollideInfo.CollisionPoint);
            
            SelectWorldInstance.CustomOctreeInstance.Delete(processData.UpdatePosition);
            SetBlockForDelete(processData.BlockX, processData.BlockY, processData.BlockZ, processData.BlockType);
            SelectWorldInstance.WorldBlockData[processData.BlockX, processData.BlockY, processData.BlockZ].bRendered = false;
            
            // 블록 파괴 사운드 재생
            PlayBlockBreakSound(processData.BlockType, processData.UpdatePosition);
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
        Action<byte> UpdateUserItem = (byte blockType) =>
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
                        dbconn.Open(); //Open connection to the database.
                        StringBuilder sqlQuery = new StringBuilder();
                        sqlQuery.AppendFormat("INSERT INTO USER_ITEM (name, type, amount, id) VALUES('{0}', '{1}', 1, {2} )",
                            itemInfo.Name, type, itemID);
                        dbcmd.CommandText = sqlQuery.ToString();
                        dbcmd.ExecuteNonQuery();

                        dbconn.Close();
                    }
                    catch (SqliteException e) // 인벤토리에 중복된 아이템이 있다면, 수량증가를 해야한다.
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
        };

        var gameWorldConfig = WorldConfigFile.Instance.GetConfig();
        if ((x < gameWorldConfig.SubWorldSizeX) &&
           (y < gameWorldConfig.SubWorldSizeY) &&
           (z < gameWorldConfig.SubWorldSizeZ) &&
           (x >= 0) && (y >= 0) && (z >= 0))
        {
            // 블록을 삭제할때마다, DB를 접속해서 쿼리 날리고 갱신시키는건 너무 무거운 작업.
            // 비동기로 처리하는게 좋을 듯 싶다.
            //UpdateUserItem(world.WorldBlockData[x, y, z].Type);
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

    private void UpdateChunkAt(int x, int y, int z, byte block, ChunkSlot[,,] chunkSlots)
    {
        var gameWorldConfig = WorldConfigFile.Instance.GetConfig();
        // world data 인덱스를 chunkGroup 인덱스로 변환한다. 
        int updateX, updateY, updateZ;
        updateX = Mathf.FloorToInt(x / chunkSize);
        updateY = Mathf.FloorToInt(y / chunkSize);
        updateZ = Mathf.FloorToInt(z / chunkSize);
        if (chunkSlots[updateX, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN] == null)
        {
            return;
        }
        chunkSlots[updateX, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;

        if (x - (chunkSize * updateX) == 0 && updateX != 0)
        {
            chunkSlots[updateX - 1, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }

        if (x - (chunkSize * updateX) == gameWorldConfig.ChunkSize && updateX != chunkSlots.GetLength(0) - 1)
        {
            chunkSlots[updateX + 1, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }

        if (y - (chunkSize * updateY) == 0 && updateY != 0)
        {
            chunkSlots[updateX, updateY - 1, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }

        if (y - (chunkSize * updateY) == gameWorldConfig.ChunkSize && updateY != chunkSlots.GetLength(1) - 1)
        {
            chunkSlots[updateX, updateY + 1, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }

        if (z - (chunkSize * updateZ) == 0 && updateZ != 0)
        {
            chunkSlots[updateX, updateY, updateZ - 1].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }

        if (z - (chunkSize * updateZ) == gameWorldConfig.ChunkSize && updateZ != chunkSlots.GetLength(2) - 1)
        {
            chunkSlots[updateX, updateY, updateZ + 1].Chunks[(int)ChunkType.TERRAIN].Update = true;
        }
    }

    private void UpdateChunkAt(int x, int y, int z, byte block)
    {
        var gameWorldConfig = WorldConfigFile.Instance.GetConfig();
        // world data 인덱스를 chunkGroup 인덱스로 변환한다. 
        int updateX, updateY, updateZ;
        updateX = Mathf.FloorToInt(x / chunkSize);
        updateY = Mathf.FloorToInt(y / chunkSize);
        updateZ = Mathf.FloorToInt(z / chunkSize);
        if (SelectWorldInstance.ChunkSlots[updateX, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN] == null)
        {
            return;
        }
        SelectWorldInstance.ChunkSlots[updateX, updateY, updateZ].Chunks[(int)ChunkType.TERRAIN].Update = true;

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
    
    /// <summary>
    /// 블록 설치 위치의 유효성을 검사합니다.
    /// </summary>
    private bool IsValidBlockPlacement(int x, int y, int z, SubWorld subWorld)
    {
        // 범위 체크
        var gameWorldConfig = WorldConfigFile.Instance.GetConfig();
        if (x < 0 || x >= gameWorldConfig.SubWorldSizeX || 
            y < 0 || y >= gameWorldConfig.SubWorldSizeY || 
            z < 0 || z >= gameWorldConfig.SubWorldSizeZ)
        {
            return false;
        }
        
        // 이미 블록이 있는지 체크
        if (subWorld.WorldBlockData[x, y, z].CurrentType != (byte)BlockTileType.EMPTY)
        {
            return false;
        }
        
        // 플레이어와 겹치는지 체크 (플레이어 위치 근처)
        if (GamePlayerManager.Instance?.GetCurrentPlayer() != null)
        {
            Vector3 playerPos = GamePlayerManager.Instance.GetCurrentPlayer().transform.position;
            Vector3 blockWorldPos = new Vector3(x, y, z);
            
            // 플레이어 바운딩 박스와 겹치는지 확인 (간단한 거리 체크)
            if (Vector3.Distance(playerPos, blockWorldPos) < MIN_BLOCK_PLACE_DISTANCE)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 블록 타입에 따른 파괴 파티클 효과를 생성합니다.
    /// </summary>
    private void SpawnBlockBreakParticles(byte blockType, Vector3 position)
    {
        ParticleEffectSpawnParams spawnParams;
        
        // 블록 타입에 따른 다른 파티클 효과
        switch ((BlockTileType)blockType)
        {
            case BlockTileType.STONE_BIG:
            case BlockTileType.STONE_SMALL:
                spawnParams.ParticleType = GameParticleType.FireworksGreenSmall;
                break;
            case BlockTileType.WOOD:
                spawnParams.ParticleType = GameParticleType.FireworksGreenSmall; // 나무용 파티클로 변경 가능
                break;
            case BlockTileType.GRASS:
                spawnParams.ParticleType = GameParticleType.FireworksGreenSmall; // 풀용 파티클로 변경 가능
                break;
            default:
                spawnParams.ParticleType = GameParticleType.FireworksGreenSmall;
                break;
        }
        
        spawnParams.SpawnLocation = position;
        spawnParams.SpawnRotation = Quaternion.identity;
        spawnParams.bLooping = false;
        spawnParams.bStart = true;
        GameParticleEffectManager.Instance.SpawnParticleEffect(spawnParams);
    }
    
    /// <summary>
    /// 블록 설치 사운드를 재생합니다.
    /// </summary>
    private void PlayBlockPlaceSound(byte blockType, Vector3 position)
    {
        // TODO: 블록 타입에 따른 다른 사운드 재생
        // GameSoundManager를 통한 사운드 재생 구현
    }
    
    /// <summary>
    /// 블록 파괴 사운드를 재생합니다.
    /// </summary>
    private void PlayBlockBreakSound(byte blockType, Vector3 position)
    {
        // TODO: 블록 타입에 따른 다른 사운드 재생
        // GameSoundManager를 통한 사운드 재생 구현
    }
}
