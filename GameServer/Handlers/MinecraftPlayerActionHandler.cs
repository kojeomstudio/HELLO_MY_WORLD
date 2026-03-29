using GameServerApp.Database;
using GameServerApp.World;
using SharedProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Google.Protobuf;
using SharedProtocol.EnhancedMinecraft;
using Enhanced = EnhancedMinecraftProtocol;

namespace GameServerApp.Handlers
{
    /// <summary>
    /// 마인크래프트 플레이어 액션을 처리하는 핸들러
    /// 블록 부수기, 설치, 아이템 사용 등의 게임플레이 액션을 담당합니다.
    /// </summary>
    public class MinecraftPlayerActionHandler : IMessageHandler
    {
        private readonly DatabaseHelper _database;
        private readonly SessionManager _sessions;
        private readonly WorldManager _worldManager;
        private readonly MinecraftMessageDispatcher _minecraftDispatcher;

        // 블록 부수기 진행도 추적 (플레이어별)
        private readonly Dictionary<string, BlockBreakInfo> _blockBreakProgress = new();
        
        // 블록 내구도 정보 (블록 타입별 부수는 시간 - 틱 단위)
        private readonly Dictionary<int, int> _blockHardness = new()
        {
            { 1, 30 },    // 돌 - 1.5초 (30틱)
            { 2, 6 },     // 흙 - 0.3초 (6틱)  
            { 3, 10 },    // 나무 - 0.5초 (10틱)
            { 4, 100 },   // 흑요석 - 5초 (100틱)
            { 5, 2 }      // 잎 - 0.1초 (2틱)
        };

        public MinecraftPlayerActionHandler(DatabaseHelper database, SessionManager sessions, 
            WorldManager worldManager, MinecraftMessageDispatcher minecraftDispatcher)
        {
            _database = database;
            _sessions = sessions;
            _worldManager = worldManager;
            _minecraftDispatcher = minecraftDispatcher;

            ProtoRuntime.EnsureInitialized();
            ProtocolRegistry.EnsureRegistered(MinecraftMessageType.PlayerActionRequest);
            ProtocolRegistry.EnsureRegistered(MinecraftMessageType.PlayerActionResponse);
            ProtocolRegistry.EnsureRegistered(MinecraftMessageType.BlockChangeNotification);
        }

        public MessageType Type => (MessageType)MinecraftMessageType.PlayerActionRequest;

        /// <summary>
        /// 플레이어 액션 요청 처리
        /// </summary>
        public async Task HandleAsync(Session session, object message)
        {
            if (message is byte[] messageData)
            {
                await HandleMinecraftActionAsync(session, messageData);
            }
            else
            {
                Console.WriteLine("Invalid message format for MinecraftPlayerActionHandler");
            }
        }

        /// <summary>
        /// 마인크래프트 액션 메시지 처리
        /// </summary>
        private async Task HandleMinecraftActionAsync(Session session, byte[] messageData)
        {
            try
            {
                var response = new PlayerActionResponseMessage();
                bool preferEnhanced = session.UseEnhancedMinecraftProtocol || LooksLikeEnhancedPlayerActionRequest(messageData);

                PlayerActionRequestMessage actionRequest;
                if (preferEnhanced && TryParseEnhancedPlayerActionRequest(messageData, out var enhancedRequest))
                {
                    session.UseEnhancedMinecraftProtocol = true;
                    actionRequest = ConvertEnhancedPlayerActionRequest(enhancedRequest!);
                }
                else
                {
                    using var stream = new MemoryStream(messageData);
                    actionRequest = ProtoBuf.Serializer.Deserialize<PlayerActionRequestMessage>(stream);
                }

                var playerState = _sessions.GetPlayerState(session.UserName!);
                if (playerState == null)
                {
                    response.Success = false;
                    response.Message = "플레이어 상태를 찾을 수 없습니다.";
                    await SendPlayerActionResponseAsync(session, response);
                    return;
                }

                // 액션 타입에 따른 처리
                switch (actionRequest.Action)
                {
                    case PlayerActionType.StartDestroyBlock:
                        await HandleStartDestroyBlock(session, actionRequest, response);
                        break;
                    
                    case PlayerActionType.StopDestroyBlock:
                        await HandleStopDestroyBlock(session, actionRequest, response);
                        break;
                    
                    case PlayerActionType.AbortDestroyBlock:
                        await HandleAbortDestroyBlock(session, actionRequest, response);
                        break;
                    
                    case PlayerActionType.PlaceBlock:
                        await HandlePlaceBlock(session, actionRequest, response);
                        break;
                    
                    case PlayerActionType.UseItem:
                        await HandleUseItem(session, actionRequest, response);
                        break;
                    
                    case PlayerActionType.DropItem:
                        await HandleDropItem(session, actionRequest, response);
                        break;
                    
                    default:
                        response.Success = false;
                        response.Message = $"지원하지 않는 액션 타입: {actionRequest.Action}";
                        break;
                }

                response.Sequence = actionRequest.Sequence;
                await SendPlayerActionResponseAsync(session, response);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"플레이어 액션 처리 오류: {ex.Message}");
                var errorResponse = new PlayerActionResponseMessage
                {
                    Success = false,
                    Message = "서버에서 액션 처리 중 오류가 발생했습니다."
                };
                await SendPlayerActionResponseAsync(session, errorResponse);
            }
        }

        /// <summary>
        /// 블록 부수기 시작
        /// </summary>
        private async Task HandleStartDestroyBlock(Session session, PlayerActionRequestMessage request, PlayerActionResponseMessage response)
        {
            var playerId = session.UserName!;
            var blockPos = request.TargetPosition;
            
            // 월드에서 블록 정보 가져오기
            var blockInfo = await _worldManager.GetBlockAsync(blockPos.X, blockPos.Y, blockPos.Z);
            if (blockInfo == null || blockInfo.BlockId == 0) // 0 = 공기 블록
            {
                response.Success = false;
                response.Message = "부술 블록이 없습니다.";
                return;
            }

            // 블록 내구도 확인
            var hardness = _blockHardness.GetValueOrDefault(blockInfo.BlockId, 20); // 기본 1초
            
            // 크리에이티브 모드라면 즉시 파괴
            var playerState = _sessions.GetPlayerState(playerId);
            if (playerState != null && (GameMode)playerState.GameMode == GameMode.Creative)
            {
                await DestroyBlockAsync(session, blockPos, blockInfo, response);
                response.Success = true;
                response.BreakProgress = 1.0f;
                return;
            }

            // 블록 부수기 진행도 초기화
            _blockBreakProgress[playerId] = new BlockBreakInfo
            {
                Position = blockPos,
                StartTime = DateTime.UtcNow,
                RequiredTicks = hardness,
                BlockId = blockInfo.BlockId
            };

            response.Success = true;
            response.BreakProgress = 0.0f;
            response.Message = "블록 부수기 시작";

            // 다른 플레이어들에게 블록 부수기 시작 알림
            await BroadcastBlockBreakStart(playerId, blockPos);
        }

        /// <summary>
        /// 블록 부수기 완료
        /// </summary>
        private async Task HandleStopDestroyBlock(Session session, PlayerActionRequestMessage request, PlayerActionResponseMessage response)
        {
            var playerId = session.UserName!;
            
            if (!_blockBreakProgress.TryGetValue(playerId, out var breakInfo))
            {
                response.Success = false;
                response.Message = "블록 부수기 진행 정보를 찾을 수 없습니다.";
                return;
            }

            // 시간이 충분히 지났는지 확인
            var elapsed = DateTime.UtcNow - breakInfo.StartTime;
            var requiredTime = TimeSpan.FromMilliseconds(breakInfo.RequiredTicks * 50); // 1틱 = 50ms

            if (elapsed >= requiredTime)
            {
                // 블록 파괴 완료
                var blockInfo = await _worldManager.GetBlockAsync(breakInfo.Position.X, breakInfo.Position.Y, breakInfo.Position.Z);
                if (blockInfo != null)
                {
                    await DestroyBlockAsync(session, breakInfo.Position, blockInfo, response);
                    response.Success = true;
                    response.BreakProgress = 1.0f;
                    response.Message = "블록 파괴 완료";
                }
                else
                {
                    response.Success = false;
                    response.Message = "블록이 이미 제거되었습니다.";
                }
            }
            else
            {
                response.Success = false;
                response.BreakProgress = (float)(elapsed.TotalMilliseconds / requiredTime.TotalMilliseconds);
                response.Message = "블록 부수기가 완료되지 않았습니다.";
            }

            // 진행도 정보 제거
            _blockBreakProgress.Remove(playerId);
        }

        /// <summary>
        /// 블록 부수기 취소
        /// </summary>
        private async Task HandleAbortDestroyBlock(Session session, PlayerActionRequestMessage request, PlayerActionResponseMessage response)
        {
            var playerId = session.UserName!;
            
            if (_blockBreakProgress.ContainsKey(playerId))
            {
                var breakInfo = _blockBreakProgress[playerId];
                _blockBreakProgress.Remove(playerId);
                
                // 다른 플레이어들에게 블록 부수기 취소 알림
                await BroadcastBlockBreakCancel(playerId, breakInfo.Position);
                
                response.Success = true;
                response.Message = "블록 부수기 취소";
            }
            else
            {
                response.Success = false;
                response.Message = "진행 중인 블록 부수기가 없습니다.";
            }
        }

        /// <summary>
        /// 블록 설치
        /// </summary>
        private async Task HandlePlaceBlock(Session session, PlayerActionRequestMessage request, PlayerActionResponseMessage response)
        {
            var playerState = _sessions.GetPlayerState(session.UserName!);
            if (playerState == null)
            {
                response.Success = false;
                response.Message = "플레이어 상태를 찾을 수 없습니다.";
                return;
            }

            // 사용한 아이템이 블록인지 확인
            if (request.UsedItem.ItemType != ItemType.Block)
            {
                response.Success = false;
                response.Message = "블록이 아닌 아이템입니다.";
                return;
            }

            // 설치할 위치 계산 (면에 따라)
            var placePos = CalculatePlacePosition(request.TargetPosition, request.Face);
            
            // 해당 위치가 비어있는지 확인
            var existingBlock = await _worldManager.GetBlockAsync(placePos.X, placePos.Y, placePos.Z);
            if (existingBlock != null && existingBlock.BlockId != 0)
            {
                response.Success = false;
                response.Message = "해당 위치에 이미 블록이 있습니다.";
                return;
            }

            // 블록 설치
            var newBlock = new Models.BlockData
            {
                BlockId = request.UsedItem.ItemId,
                X = placePos.X,
                Y = placePos.Y,
                Z = placePos.Z,
                Metadata = 0,
                PlacedBy = session.UserName!,
                PlacedAt = DateTime.UtcNow
            };

            await _worldManager.SetBlockAsync(newBlock);

            // 인벤토리에서 아이템 제거 (크리에이티브 모드가 아닌 경우)
            if ((GameMode)playerState.GameMode != GameMode.Creative)
            {
                // TODO: 인벤토리에서 아이템 수량 감소 구현
            }

            response.Success = true;
            response.Message = "블록 설치 완료";

            // 다른 플레이어들에게 블록 변경 알림
            await BroadcastBlockChange(session.UserName!, placePos, 0, request.UsedItem.ItemId, Enhanced.ChangeReason.PlayerPlace);
        }

        /// <summary>
        /// 아이템 사용
        /// </summary>
        private async Task HandleUseItem(Session session, PlayerActionRequestMessage request, PlayerActionResponseMessage response)
        {
            // TODO: 아이템 타입에 따른 사용 로직 구현
            // 음식: 허기 회복
            // 도구: 내구도 감소
            // 포션: 효과 적용
            
            response.Success = true;
            response.Message = "아이템 사용 (구현 예정)";
        }

        /// <summary>
        /// 아이템 드롭
        /// </summary>
        private async Task HandleDropItem(Session session, PlayerActionRequestMessage request, PlayerActionResponseMessage response)
        {
            var playerState = _sessions.GetPlayerState(session.UserName!);
            if (playerState == null || request.UsedItem.Quantity <= 0)
            {
                response.Success = false;
                response.Message = "드롭할 아이템이 없습니다.";
                return;
            }

            // 드롭된 아이템 엔티티 생성
            var dropInfo = new ItemDropInfo
            {
                Item = request.UsedItem,
                DropPosition = new Vector3D(playerState.Position.X, playerState.Position.Y, playerState.Position.Z),
                Velocity = new Vector3D(0, 0.3, 0), // 위로 살짝 던지기
                EntityId = Guid.NewGuid().ToString()
            };

            response.DroppedItems.Add(dropInfo);
            response.Success = true;
            response.Message = "아이템 드롭 완료";

            // 다른 플레이어들에게 아이템 드롭 알림
            // TODO: 엔티티 스폰 브로드캐스트
        }

        /// <summary>
        /// 실제 블록 파괴 처리 및 드롭 아이템 생성
        /// </summary>
        private async Task DestroyBlockAsync(Session session, Vector3I position, Models.BlockData blockInfo, PlayerActionResponseMessage response)
        {
            await _worldManager.RemoveBlockAsync(position.X, position.Y, position.Z);

            List<ItemDropInfo>? drops = null;
            var playerState = _sessions.GetPlayerState(session.UserName!);
            if (playerState != null && (GameMode)playerState.GameMode == GameMode.Survival)
            {
                var dropItem = CreateBlockDrop(blockInfo.BlockId);
                if (dropItem != null)
                {
                    var dropInfo = new ItemDropInfo
                    {
                        Item = dropItem,
                        DropPosition = new Vector3D(position.X + 0.5, position.Y + 0.5, position.Z + 0.5),
                        Velocity = new Vector3D(0, 0.2, 0),
                        EntityId = Guid.NewGuid().ToString()
                    };

                    drops = new List<ItemDropInfo> { dropInfo };
                    response.DroppedItems.Add(dropInfo);
                }
            }

            await BroadcastBlockChange(session.UserName!, position, blockInfo.BlockId, 0, Enhanced.ChangeReason.PlayerBreak, drops);
        }

        /// <summary>
        /// 블록 파괴 시 드롭될 아이템 생성
        /// </summary>
        private InventoryItemInfo? CreateBlockDrop(int blockId)
        {
            // 블록별 드롭 테이블 (간단한 버전)
            return blockId switch
            {
                1 => new InventoryItemInfo { ItemId = 4, ItemName = "Cobblestone", Quantity = 1, ItemType = ItemType.Block }, // 돌 -> 조약돌
                2 => new InventoryItemInfo { ItemId = 2, ItemName = "Dirt", Quantity = 1, ItemType = ItemType.Block }, // 흙 -> 흙
                3 => new InventoryItemInfo { ItemId = 3, ItemName = "Wood", Quantity = 1, ItemType = ItemType.Block }, // 나무 -> 나무
                5 => null, // 잎은 가끔만 드롭
                _ => new InventoryItemInfo { ItemId = blockId, ItemName = $"Block_{blockId}", Quantity = 1, ItemType = ItemType.Block }
            };
        }

        /// <summary>
        /// 면에 따른 블록 설치 위치 계산
        /// </summary>
        private Vector3I CalculatePlacePosition(Vector3I targetPos, int face)
        {
            return face switch
            {
                0 => new Vector3I(targetPos.X, targetPos.Y - 1, targetPos.Z), // 아래
                1 => new Vector3I(targetPos.X, targetPos.Y + 1, targetPos.Z), // 위
                2 => new Vector3I(targetPos.X, targetPos.Y, targetPos.Z - 1), // 북
                3 => new Vector3I(targetPos.X, targetPos.Y, targetPos.Z + 1), // 남
                4 => new Vector3I(targetPos.X - 1, targetPos.Y, targetPos.Z), // 서
                5 => new Vector3I(targetPos.X + 1, targetPos.Y, targetPos.Z), // 동
                _ => targetPos
            };
        }

        /// <summary>
        /// 응답 메시지 전송
        /// </summary>
        private Task SendPlayerActionResponseAsync(Session session, PlayerActionResponseMessage response)
        {
            if (session.UseEnhancedMinecraftProtocol)
            {
                var enhancedResponse = BuildEnhancedPlayerActionResponse(response);
                return session.SendAsync((int)MinecraftMessageType.PlayerActionResponse, enhancedResponse.ToByteArray());
            }

            return SendResponseAsync(session, response);
        }

        private async Task SendResponseAsync(Session session, PlayerActionResponseMessage response)
        {
            using var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, response);
            await session.SendAsync((int)MinecraftMessageType.PlayerActionResponse, stream.ToArray());
        }

        private static bool LooksLikeEnhancedPlayerActionRequest(byte[] messageData)
        {
            if (messageData == null || messageData.Length == 0)
            {
                return false;
            }

            try
            {
                var input = new CodedInputStream(messageData);
                uint tag;

                while ((tag = input.ReadTag()) != 0)
                {
                    int fieldNumber = WireFormat.GetTagFieldNumber(tag);
                    if (fieldNumber == 7)
                    {
                        return WireFormat.GetTagWireType(tag) == WireFormat.WireType.LengthDelimited;
                    }

                    input.SkipLastField();
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryParseEnhancedPlayerActionRequest(byte[] messageData, out Enhanced.PlayerActionRequest? request)
        {
            try
            {
                request = Enhanced.PlayerActionRequest.Parser.ParseFrom(messageData);
                if (request == null || request.TargetPosition == null)
                {
                    request = null;
                    return false;
                }

                return true;
            }
            catch (InvalidProtocolBufferException)
            {
                request = null;
                return false;
            }
        }

        private static PlayerActionRequestMessage ConvertEnhancedPlayerActionRequest(Enhanced.PlayerActionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return new PlayerActionRequestMessage
            {
                Action = request.Action switch
                {
                    Enhanced.PlayerAction.StartDestroyBlock => PlayerActionType.StartDestroyBlock,
                    Enhanced.PlayerAction.AbortDestroyBlock => PlayerActionType.AbortDestroyBlock,
                    Enhanced.PlayerAction.FinishDestroyBlock => PlayerActionType.StopDestroyBlock,
                    Enhanced.PlayerAction.PlaceBlock => PlayerActionType.PlaceBlock,
                    Enhanced.PlayerAction.RightClickBlock => PlayerActionType.RightClickBlock,
                    Enhanced.PlayerAction.UseItem => PlayerActionType.UseItem,
                    Enhanced.PlayerAction.DropItem => PlayerActionType.DropItem,
                    Enhanced.PlayerAction.DropItemStack => PlayerActionType.DropItem,
                    _ => PlayerActionType.UseItem
                },
                TargetPosition = new Vector3I(request.TargetPosition.X, request.TargetPosition.Y, request.TargetPosition.Z),
                Face = request.Face,
                CursorPosition = request.CursorPosition == null
                    ? new Vector3D()
                    : new Vector3D(request.CursorPosition.X, request.CursorPosition.Y, request.CursorPosition.Z),
                UsedItem = request.UsedItem == null
                    ? new InventoryItemInfo()
                    : ConvertToLegacyInventoryItem(request.UsedItem),
                Sequence = request.Sequence,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        private static InventoryItemInfo ConvertToLegacyInventoryItem(Enhanced.ItemStack item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            return new InventoryItemInfo
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName ?? string.Empty,
                Quantity = item.Count,
                Durability = item.Durability,
                MaxDurability = item.MaxDurability,
                CustomData = item.NbtData ?? string.Empty,
                ItemType = item.ItemType switch
                {
                    Enhanced.ItemType.Block => ItemType.Block,
                    Enhanced.ItemType.Tool => ItemType.Tool,
                    Enhanced.ItemType.Weapon => ItemType.Weapon,
                    Enhanced.ItemType.Armor => ItemType.Armor,
                    Enhanced.ItemType.Food => ItemType.Food,
                    Enhanced.ItemType.Material => ItemType.Material,
                    _ => ItemType.Misc
                }
            };
        }

        private static Enhanced.PlayerActionResponse BuildEnhancedPlayerActionResponse(PlayerActionResponseMessage legacy)
        {
            if (legacy == null) throw new ArgumentNullException(nameof(legacy));

            var result = new Enhanced.ActionResult
            {
                HealthChange = 0f,
                HungerChange = 0f,
                ExperienceChange = 0
            };

            if (legacy.UpdatedItems != null)
            {
                foreach (var updated in legacy.UpdatedItems)
                {
                    if (updated == null)
                    {
                        continue;
                    }

                    result.UpdatedItems.Add(ConvertToEnhancedItemStack(updated));
                }
            }

            return new Enhanced.PlayerActionResponse
            {
                Success = legacy.Success,
                Message = legacy.Message ?? string.Empty,
                Sequence = legacy.Sequence,
                Result = result
            };
        }

        private static Enhanced.ItemStack ConvertToEnhancedItemStack(InventoryItemInfo item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            return new Enhanced.ItemStack
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName ?? string.Empty,
                Count = item.Quantity,
                Durability = item.Durability,
                MaxDurability = item.MaxDurability,
                NbtData = item.CustomData ?? string.Empty,
                ItemType = item.ItemType switch
                {
                    ItemType.Block => Enhanced.ItemType.Block,
                    ItemType.Tool => Enhanced.ItemType.Tool,
                    ItemType.Weapon => Enhanced.ItemType.Weapon,
                    ItemType.Armor => Enhanced.ItemType.Armor,
                    ItemType.Food => Enhanced.ItemType.Food,
                    ItemType.Material => Enhanced.ItemType.Material,
                    _ => Enhanced.ItemType.Misc
                },
                Rarity = Enhanced.ItemRarity.Common
            };
        }

        /// <summary>
        /// 블록 변경 브로드캐스트
        /// </summary>
        private async Task BroadcastBlockChange(string playerName, Vector3I position, int oldBlockId, int newBlockId, Enhanced.ChangeReason reason, IReadOnlyCollection<ItemDropInfo>? drops = null)
        {
            var notification = new BlockChangeNotificationMessage
            {
                Position = position,
                OldBlockId = oldBlockId,
                NewBlockId = newBlockId,
                PlayerName = playerName,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            if (drops != null)
            {
                notification.Drops.AddRange(drops);
            }

            var broadcast = new Enhanced.BlockChangeBroadcast
            {
                Position = new MinecraftGame.Common.Vector3Int { X = position.X, Y = position.Y, Z = position.Z },
                OldBlockId = oldBlockId,
                NewBlockId = newBlockId,
                Metadata = 0,
                PlayerId = playerName,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Reason = reason
            };

            if (drops != null)
            {
                foreach (var drop in drops)
                {
                    if (drop?.Item == null)
                    {
                        continue;
                    }

                    broadcast.Drops.Add(ConvertToEnhancedItemStack(drop.Item));
                }
            }

            var playerState = _sessions.GetPlayerState(playerName);
            var worldId = playerState?.CurrentWorldId ?? 1;
            var chunkX = position.X >> 4;
            var chunkZ = position.Z >> 4;

            await _sessions.BroadcastMinecraftToAreaDualAsync(worldId, chunkX, chunkZ, MinecraftMessageType.BlockChangeNotification, notification, broadcast);

            Console.WriteLine($"Block changed at [{position.X}, {position.Y}, {position.Z}]: {oldBlockId} -> {newBlockId} by {playerName}");
        }

        /// <summary>
        /// 블록 부수기 시작 알림
        /// </summary>
        private async Task BroadcastBlockBreakStart(string playerId, Vector3I position)
        {
            Console.WriteLine($"Player {playerId} started breaking block at [{position.X}, {position.Y}, {position.Z}]");
        }

        /// <summary>
        /// 블록 부수기 취소 알림
        /// </summary>
        private async Task BroadcastBlockBreakCancel(string playerId, Vector3I position)
        {
            Console.WriteLine($"Player {playerId} cancelled breaking block at [{position.X}, {position.Y}, {position.Z}]");
        }

        /// <summary>
        /// 블록 부수기 진행 정보
        /// </summary>
        private class BlockBreakInfo
        {
            public Vector3I Position { get; set; } = new();
            public DateTime StartTime { get; set; }
            public int RequiredTicks { get; set; }
            public int BlockId { get; set; }
        }
    }
}
