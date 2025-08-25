using Microsoft.Data.Sqlite;
using System.Collections.Generic;
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
                    Name TEXT NOT NULL,
                    X REAL NOT NULL,
                    Y REAL NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Maps (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void SavePlayer(Character player)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO Players (Name, X, Y) VALUES ($name, $x, $y);";
            cmd.Parameters.AddWithValue("$name", player.Name);
            cmd.Parameters.AddWithValue("$x", player.X);
            cmd.Parameters.AddWithValue("$y", player.Y);
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<Character> GetPlayers()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Name, X, Y FROM Players;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new Character(reader.GetString(0), reader.GetDouble(1), reader.GetDouble(2));
            }
        }

        public void SaveMap(Map map)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Maps (Name) VALUES ($name);";
            cmd.Parameters.AddWithValue("$name", map.Name);
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<Map> GetMaps()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Maps;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new Map(reader.GetInt32(0), reader.GetString(1));
            }
        }
    }
}
