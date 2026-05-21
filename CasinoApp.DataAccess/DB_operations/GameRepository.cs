using System;
using System.Collections.Generic;
using System.Text;
using CasinoApp.DataAccess.Entities;

namespace CasinoApp.DataAccess.DB_operations
{
    public class GameRepository
    {
        public List<Game> GetActiveGames()
        {
            var games = new List<Game>();

            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, Type, MinBet, MaxBet, IsActive
                FROM Games
                WHERE IsActive = 1;
            ";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                games.Add(new Game
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Type = reader.GetString(2),
                    MinBet = reader.GetDouble(3),
                    MaxBet = reader.GetDouble(4),
                    IsActive = reader.GetInt32(5) == 1
                });
            }

            return games;
        }

        public Game? GetById(int id)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, Type, MinBet, MaxBet, IsActive
                FROM Games
                WHERE Id = $id;
            ";

            command.Parameters.AddWithValue("$id", id);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Game
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Type = reader.GetString(2),
                MinBet = reader.GetDouble(3),
                MaxBet = reader.GetDouble(4),
                IsActive = reader.GetInt32(5) == 1
            };
        }

        public Game? GetByName(string name)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, Type, MinBet, MaxBet, IsActive
                FROM Games
                WHERE Name = $name;
            ";

            command.Parameters.AddWithValue("$name", name);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Game
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Type = reader.GetString(2),
                MinBet = reader.GetDouble(3),
                MaxBet = reader.GetDouble(4),
                IsActive = reader.GetInt32(5) == 1
            };
        }
    }
}
