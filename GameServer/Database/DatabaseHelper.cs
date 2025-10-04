using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using GameServerApp.Models;

namespace GameServerApp.Database
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string databaseFile)
        {
            _connectionString = new SqliteConnectionStringBuilder { DataSource = databaseFile }.ToString();
            Initialize();
        }

        private void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Players (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    Salt TEXT NOT NULL,
                    X REAL NOT NULL DEFAULT 0,
                    Y REAL NOT NULL DEFAULT 100,
                    Z REAL NOT NULL DEFAULT 0,
                    Level INTEGER NOT NULL DEFAULT 1,
                    Health INTEGER NOT NULL DEFAULT 100,
                    MaxHealth INTEGER NOT NULL DEFAULT 100,
                    Experience INTEGER NOT NULL DEFAULT 0,
                    GameMode INTEGER NOT NULL DEFAULT 0,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    LastLoginAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    IsOnline INTEGER NOT NULL DEFAULT 0
                );
                
                CREATE TABLE IF NOT EXISTS PlayerInventories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayerId INTEGER NOT NULL,
                    ItemId INTEGER NOT NULL,
                    ItemName TEXT NOT NULL,
                    Quantity INTEGER NOT NULL DEFAULT 1,
                    Slot INTEGER NOT NULL,
                    FOREIGN KEY (PlayerId) REFERENCES Players(Id) ON DELETE CASCADE,
                    UNIQUE(PlayerId, Slot)
                );
                
                CREATE TABLE IF NOT EXISTS PlayerInventorySnapshots (
                    PlayerName TEXT PRIMARY KEY,
                    InventoryJson TEXT NOT NULL,
                    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
                CREATE INDEX IF NOT EXISTS idx_inventory_snapshots_updated_at ON PlayerInventorySnapshots(UpdatedAt);
                
                CREATE TABLE IF NOT EXISTS Worlds (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    Description TEXT,
                    Seed BIGINT NOT NULL,
                    WorldType INTEGER NOT NULL DEFAULT 0,
                    Difficulty INTEGER NOT NULL DEFAULT 1,
                    MaxPlayers INTEGER NOT NULL DEFAULT 20,
                    SpawnX REAL NOT NULL DEFAULT 0,
                    SpawnY REAL NOT NULL DEFAULT 100,
                    SpawnZ REAL NOT NULL DEFAULT 0,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );
                
                CREATE TABLE IF NOT EXISTS Chunks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    WorldId INTEGER NOT NULL,
                    ChunkX INTEGER NOT NULL,
                    ChunkZ INTEGER NOT NULL,
                    BlockData BLOB,
                    BiomeData BLOB,
                    LastModified DATETIME DEFAULT CURRENT_TIMESTAMP,
                    IsLoaded INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (WorldId) REFERENCES Worlds(Id) ON DELETE CASCADE,
                    UNIQUE(WorldId, ChunkX, ChunkZ)
                );
                
                CREATE TABLE IF NOT EXISTS BlockChanges (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    WorldId INTEGER NOT NULL,
                    ChunkX INTEGER NOT NULL,
                    ChunkZ INTEGER NOT NULL,
                    BlockX INTEGER NOT NULL,
                    BlockY INTEGER NOT NULL,
                    BlockZ INTEGER NOT NULL,
                    BlockType INTEGER NOT NULL,
                    PlayerId INTEGER NOT NULL,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (WorldId) REFERENCES Worlds(Id) ON DELETE CASCADE,
                    FOREIGN KEY (PlayerId) REFERENCES Players(Id)
                );
                
                CREATE TABLE IF NOT EXISTS PlayerSessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayerId INTEGER NOT NULL,
                    SessionToken TEXT NOT NULL UNIQUE,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    ExpiresAt DATETIME NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    FOREIGN KEY (PlayerId) REFERENCES Players(Id) ON DELETE CASCADE
                );
                
                CREATE INDEX IF NOT EXISTS idx_chunks_world_pos ON Chunks(WorldId, ChunkX, ChunkZ);
                CREATE INDEX IF NOT EXISTS idx_block_changes_world_chunk ON BlockChanges(WorldId, ChunkX, ChunkZ);
                CREATE INDEX IF NOT EXISTS idx_player_sessions_token ON PlayerSessions(SessionToken);
                CREATE INDEX IF NOT EXISTS idx_players_name ON Players(Name);";
            cmd.ExecuteNonQuery();
            
            CreateDefaultWorld();
        }
        
        private void CreateDefaultWorld()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO Worlds (Name, Description, Seed, WorldType, Difficulty, SpawnX, SpawnY, SpawnZ)
                VALUES ('default', 'Default Minecraft World', 12345, 0, 1, 0, 100, 0);";
            cmd.ExecuteNonQuery();
        }

        private async Task ExecuteAsync(Func<SqliteConnection, Task> work)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await work(connection);
        }

        private async Task<T> ExecuteAsync<T>(Func<SqliteConnection, Task<T>> work)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await work(connection);
        }

        private async Task ExecuteInTransactionAsync(Func<SqliteConnection, SqliteTransaction, Task> work)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var dbTransaction = await connection.BeginTransactionAsync();
            var transaction = (SqliteTransaction)dbTransaction;
            try
            {
                await work(connection, transaction);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Character?> GetPlayerByNameAsync(string name)
        {
            return await ExecuteAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Name, PasswordHash, Salt, X, Y, Z, Level, Health, MaxHealth, 
                           Experience, GameMode, CreatedAt, LastLoginAt, IsOnline 
                    FROM Players WHERE Name = $name;";
                cmd.Parameters.AddWithValue("$name", name);
                
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int idxName = reader.GetOrdinal("Name");
                    int idxX = reader.GetOrdinal("X");
                    int idxY = reader.GetOrdinal("Y");
                    int idxZ = reader.GetOrdinal("Z");
                    int idxPasswordHash = reader.GetOrdinal("PasswordHash");
                    int idxSalt = reader.GetOrdinal("Salt");
                    int idxLevel = reader.GetOrdinal("Level");
                    int idxHealth = reader.GetOrdinal("Health");
                    int idxMaxHealth = reader.GetOrdinal("MaxHealth");
                    int idxCreatedAt = reader.GetOrdinal("CreatedAt");
                    int idxLastLoginAt = reader.GetOrdinal("LastLoginAt");
                    int idxId = reader.GetOrdinal("Id");

                    var character = new Character(reader.GetString(idxName), 
                        reader.GetDouble(idxX), reader.GetDouble(idxY), reader.GetDouble(idxZ))
                    {
                        PasswordHash = reader.GetString(idxPasswordHash),
                        Salt = reader.GetString(idxSalt),
                        Level = reader.GetInt32(idxLevel),
                        Health = reader.GetInt32(idxHealth),
                        MaxHealth = reader.GetInt32(idxMaxHealth),
                        CreatedAt = reader.GetDateTime(idxCreatedAt),
                        LastLoginAt = reader.GetDateTime(idxLastLoginAt)
                    };
                    
                    await LoadPlayerInventory(character, reader.GetInt32(idxId), connection);
                    return character;
                }
                return null;
            });
        }
        
        private async Task LoadPlayerInventory(Character character, int playerId, SqliteConnection connection, SqliteTransaction? transaction = null)
        {
            var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                SELECT ItemId, ItemName, Quantity, Slot 
                FROM PlayerInventories 
                WHERE PlayerId = $playerId 
                ORDER BY Slot;";
            cmd.Parameters.AddWithValue("$playerId", playerId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int idxItemId = reader.GetOrdinal("ItemId");
                int idxItemName = reader.GetOrdinal("ItemName");
                int idxQuantity = reader.GetOrdinal("Quantity");

                character.AddItem(reader.GetInt32(idxItemId), 
                    reader.GetString(idxItemName), reader.GetInt32(idxQuantity));
            }
        }

        public async Task SavePlayerAsync(Character player)
        {
            await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    INSERT INTO Players (Name, PasswordHash, Salt, X, Y, Z, Level, Health, MaxHealth, 
                                       Experience, GameMode, LastLoginAt, IsOnline) 
                    VALUES ($name, $passwordHash, $salt, $x, $y, $z, $level, $health, $maxHealth, 
                           $experience, $gameMode, $lastLogin, $isOnline)
                    ON CONFLICT(Name) DO UPDATE SET 
                        X = excluded.X, Y = excluded.Y, Z = excluded.Z,
                        Level = excluded.Level, Health = excluded.Health, MaxHealth = excluded.MaxHealth,
                        Experience = excluded.Experience, GameMode = excluded.GameMode,
                        LastLoginAt = excluded.LastLoginAt, IsOnline = excluded.IsOnline;";
                
                cmd.Parameters.AddWithValue("$name", player.Name);
                cmd.Parameters.AddWithValue("$passwordHash", player.PasswordHash);
                cmd.Parameters.AddWithValue("$salt", player.Salt);
                cmd.Parameters.AddWithValue("$x", player.X);
                cmd.Parameters.AddWithValue("$y", player.Y);
                cmd.Parameters.AddWithValue("$z", player.Z);
                cmd.Parameters.AddWithValue("$level", player.Level);
                cmd.Parameters.AddWithValue("$health", player.Health);
                cmd.Parameters.AddWithValue("$maxHealth", player.MaxHealth);
                cmd.Parameters.AddWithValue("$experience", 0);
                cmd.Parameters.AddWithValue("$gameMode", 0);
                cmd.Parameters.AddWithValue("$lastLogin", player.LastLoginAt);
                cmd.Parameters.AddWithValue("$isOnline", 1);
                
                await cmd.ExecuteNonQueryAsync();
                
                var playerId = await GetPlayerIdByName(player.Name, connection, transaction);
                await SavePlayerInventory(playerId, player.Inventory, connection, transaction);
            });
        }
        
        private async Task<int> GetPlayerIdByName(string name, SqliteConnection connection, SqliteTransaction? transaction)
        {
            var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "SELECT Id FROM Players WHERE Name = $name;";
            cmd.Parameters.AddWithValue("$name", name);
            
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException($"Player '{name}' was not found in the database.");
            }
            return Convert.ToInt32(result);
        }
        
        private async Task SavePlayerInventory(int playerId, List<Item> inventory, SqliteConnection connection, SqliteTransaction transaction)
        {
            var deleteCmd = connection.CreateCommand();
            deleteCmd.Transaction = transaction;
            deleteCmd.CommandText = "DELETE FROM PlayerInventories WHERE PlayerId = $playerId;";
            deleteCmd.Parameters.AddWithValue("$playerId", playerId);
            await deleteCmd.ExecuteNonQueryAsync();
            
            var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = transaction;
            insertCmd.CommandText = @"
                INSERT INTO PlayerInventories (PlayerId, ItemId, ItemName, Quantity, Slot)
                VALUES ($playerId, $itemId, $itemName, $quantity, $slot);";
            insertCmd.Parameters.AddWithValue("$playerId", playerId);
            var itemIdParam = insertCmd.Parameters.Add("$itemId", SqliteType.Integer);
            var itemNameParam = insertCmd.Parameters.Add("$itemName", SqliteType.Text);
            var quantityParam = insertCmd.Parameters.Add("$quantity", SqliteType.Integer);
            var slotParam = insertCmd.Parameters.Add("$slot", SqliteType.Integer);

            for (int i = 0; i < inventory.Count; i++)
            {
                var item = inventory[i];
                itemIdParam.Value = item.Id;
                itemNameParam.Value = item.Name;
                quantityParam.Value = item.Quantity;
                slotParam.Value = i;
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<string?> LoadInventorySnapshotAsync(string playerName)
        {
            return await ExecuteAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT InventoryJson FROM PlayerInventorySnapshots WHERE PlayerName = $playerName;";
                cmd.Parameters.AddWithValue("$playerName", playerName);

                var result = await cmd.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? null : Convert.ToString(result);
            });
        }

        public async Task SaveInventorySnapshotAsync(string playerName, string snapshotJson)
        {
            await ExecuteAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO PlayerInventorySnapshots (PlayerName, InventoryJson, UpdatedAt)
                    VALUES ($playerName, $inventoryJson, CURRENT_TIMESTAMP)
                    ON CONFLICT(PlayerName) DO UPDATE SET
                        InventoryJson = excluded.InventoryJson,
                        UpdatedAt = excluded.UpdatedAt;";
                cmd.Parameters.AddWithValue("$playerName", playerName);
                cmd.Parameters.AddWithValue("$inventoryJson", snapshotJson);
                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task<string> CreateSessionAsync(string playerName)
        {
            return await ExecuteAsync(async connection =>
            {
                var playerId = await GetPlayerIdByName(playerName, connection, null);
                var sessionToken = Guid.NewGuid().ToString();
                var expiresAt = DateTime.UtcNow.AddHours(24);
                
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO PlayerSessions (PlayerId, SessionToken, ExpiresAt)
                    VALUES ($playerId, $sessionToken, $expiresAt);";
                cmd.Parameters.AddWithValue("$playerId", playerId);
                cmd.Parameters.AddWithValue("$sessionToken", sessionToken);
                cmd.Parameters.AddWithValue("$expiresAt", expiresAt);
                
                await cmd.ExecuteNonQueryAsync();
                return sessionToken;
            });
        }
        
        public async Task<bool> ValidateSessionAsync(string sessionToken)
        {
            return await ExecuteAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM PlayerSessions 
                    WHERE SessionToken = $token AND ExpiresAt > datetime('now') AND IsActive = 1;";
                cmd.Parameters.AddWithValue("$token", sessionToken);
                
                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return count > 0;
            });
        }

        public async Task SaveChunkAsync(int worldId, int chunkX, int chunkZ, byte[] blockData, byte[]? biomeData = null)
        {
            await ExecuteAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Chunks (WorldId, ChunkX, ChunkZ, BlockData, BiomeData, IsLoaded)
                    VALUES ($worldId, $chunkX, $chunkZ, $blockData, $biomeData, 1)
                    ON CONFLICT(WorldId, ChunkX, ChunkZ) DO UPDATE SET
                        BlockData = excluded.BlockData,
                        BiomeData = excluded.BiomeData,
                        LastModified = CURRENT_TIMESTAMP,
                        IsLoaded = 1;";
                
                cmd.Parameters.AddWithValue("$worldId", worldId);
                cmd.Parameters.AddWithValue("$chunkX", chunkX);
                cmd.Parameters.AddWithValue("$chunkZ", chunkZ);
                cmd.Parameters.AddWithValue("$blockData", blockData);
                cmd.Parameters.Add("$biomeData", SqliteType.Blob).Value = biomeData ?? (object)DBNull.Value;
                
                await cmd.ExecuteNonQueryAsync();
            });
        }
        
        public async Task<(byte[] blockData, byte[] biomeData)?> LoadChunkAsync(int worldId, int chunkX, int chunkZ)
        {
            return await ExecuteAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT BlockData, BiomeData FROM Chunks 
                    WHERE WorldId = $worldId AND ChunkX = $chunkX AND ChunkZ = $chunkZ;";
                cmd.Parameters.AddWithValue("$worldId", worldId);
                cmd.Parameters.AddWithValue("$chunkX", chunkX);
                cmd.Parameters.AddWithValue("$chunkZ", chunkZ);
                
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var blockData = reader["BlockData"] as byte[] ?? Array.Empty<byte>();
                    var biomeData = reader["BiomeData"] as byte[] ?? Array.Empty<byte>();
                    return (blockData, biomeData);
                }
                
                return ((byte[] blockData, byte[] biomeData)?)null;
            });
        }

        public async Task<byte[]?> GetChunkDataAsync(int chunkX, int chunkZ)
        {
            var worldId = await GetDefaultWorldIdAsync();
            var result = await LoadChunkAsync(worldId, chunkX, chunkZ);
            return result?.blockData;
        }

        public async Task SaveChunkDataAsync(int chunkX, int chunkZ, byte[] blockData, byte[]? biomeData = null)
        {
            var worldId = await GetDefaultWorldIdAsync();
            await SaveChunkAsync(worldId, chunkX, chunkZ, blockData, biomeData);
        }
        
        public async Task SaveBlockChangeAsync(int worldId, int chunkX, int chunkZ, 
            int blockX, int blockY, int blockZ, int blockType, int playerId)
        {
            await ExecuteAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO BlockChanges (WorldId, ChunkX, ChunkZ, BlockX, BlockY, BlockZ, BlockType, PlayerId)
                    VALUES ($worldId, $chunkX, $chunkZ, $blockX, $blockY, $blockZ, $blockType, $playerId);";
                
                cmd.Parameters.AddWithValue("$worldId", worldId);
                cmd.Parameters.AddWithValue("$chunkX", chunkX);
                cmd.Parameters.AddWithValue("$chunkZ", chunkZ);
                cmd.Parameters.AddWithValue("$blockX", blockX);
                cmd.Parameters.AddWithValue("$blockY", blockY);
                cmd.Parameters.AddWithValue("$blockZ", blockZ);
                cmd.Parameters.AddWithValue("$blockType", blockType);
                cmd.Parameters.AddWithValue("$playerId", playerId);
                
                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task<int> GetDefaultWorldIdAsync()
        {
            return await ExecuteAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id FROM Worlds WHERE Name = 'default' LIMIT 1;";
                
                var result = await cmd.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 1;
            });
        }
    }
}
