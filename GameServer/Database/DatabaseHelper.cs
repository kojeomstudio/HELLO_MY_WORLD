using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using GameServerApp.Models;
using System.Text.Json;

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

        public async Task<Character?> GetPlayerByNameAsync(string name)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, PasswordHash, Salt, X, Y, Z, Level, Health, MaxHealth, 
                       Experience, GameMode, CreatedAt, LastLoginAt, IsOnline 
                FROM Players WHERE Name = $name;";
            cmd.Parameters.AddWithValue("$name", name);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var character = new Character(reader.GetString("Name"), 
                    reader.GetDouble("X"), reader.GetDouble("Y"), reader.GetDouble("Z"))
                {
                    PasswordHash = reader.GetString("PasswordHash"),
                    Salt = reader.GetString("Salt"),
                    Level = reader.GetInt32("Level"),
                    Health = reader.GetInt32("Health"),
                    MaxHealth = reader.GetInt32("MaxHealth"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    LastLoginAt = reader.GetDateTime("LastLoginAt")
                };
                
                await LoadPlayerInventory(character, reader.GetInt32("Id"));
                return character;
            }
            return null;
        }
        
        private async Task LoadPlayerInventory(Character character, int playerId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT ItemId, ItemName, Quantity, Slot 
                FROM PlayerInventories 
                WHERE PlayerId = $playerId 
                ORDER BY Slot;";
            cmd.Parameters.AddWithValue("$playerId", playerId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                character.AddItem(reader.GetInt32("ItemId"), 
                    reader.GetString("ItemName"), reader.GetInt32("Quantity"));
            }
        }

        public async Task SavePlayerAsync(Character player)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            using var transaction = connection.BeginTransaction();
            try
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
                
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        
        private async Task<int> GetPlayerIdByName(string name, SqliteConnection connection, SqliteTransaction transaction)
        {
            var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "SELECT Id FROM Players WHERE Name = $name;";
            cmd.Parameters.AddWithValue("$name", name);
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        
        private async Task SavePlayerInventory(int playerId, List<Item> inventory, SqliteConnection connection, SqliteTransaction transaction)
        {
            var deleteCmd = connection.CreateCommand();
            deleteCmd.Transaction = transaction;
            deleteCmd.CommandText = "DELETE FROM PlayerInventories WHERE PlayerId = $playerId;";
            deleteCmd.Parameters.AddWithValue("$playerId", playerId);
            await deleteCmd.ExecuteNonQueryAsync();
            
            for (int i = 0; i < inventory.Count; i++)
            {
                var item = inventory[i];
                var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
                    INSERT INTO PlayerInventories (PlayerId, ItemId, ItemName, Quantity, Slot)
                    VALUES ($playerId, $itemId, $itemName, $quantity, $slot);";
                insertCmd.Parameters.AddWithValue("$playerId", playerId);
                insertCmd.Parameters.AddWithValue("$itemId", item.Id);
                insertCmd.Parameters.AddWithValue("$itemName", item.Name);
                insertCmd.Parameters.AddWithValue("$quantity", item.Quantity);
                insertCmd.Parameters.AddWithValue("$slot", i);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<string> CreateSessionAsync(string playerName)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
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
        }
        
        public async Task<bool> ValidateSessionAsync(string sessionToken)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) FROM PlayerSessions 
                WHERE SessionToken = $token AND ExpiresAt > datetime('now') AND IsActive = 1;";
            cmd.Parameters.AddWithValue("$token", sessionToken);
            
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task SaveChunkAsync(int worldId, int chunkX, int chunkZ, byte[] blockData, byte[] biomeData = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
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
            cmd.Parameters.AddWithValue("$biomeData", biomeData ?? Array.Empty<byte>());
            
            await cmd.ExecuteNonQueryAsync();
        }
        
        public async Task<(byte[] blockData, byte[] biomeData)?> LoadChunkAsync(int worldId, int chunkX, int chunkZ)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
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
                var blockData = (byte[])reader["BlockData"];
                var biomeData = (byte[])reader["BiomeData"];
                return (blockData, biomeData);
            }
            
            return null;
        }
        
        public async Task SaveBlockChangeAsync(int worldId, int chunkX, int chunkZ, 
            int blockX, int blockY, int blockZ, int blockType, int playerId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
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
        }

        public async Task<int> GetDefaultWorldIdAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id FROM Worlds WHERE Name = 'default' LIMIT 1;";
            
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 1;
        }
    }
}
