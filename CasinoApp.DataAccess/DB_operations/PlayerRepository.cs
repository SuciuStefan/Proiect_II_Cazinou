using CasinoApp.DataAccess.Entities;
using Microsoft.Data.Sqlite;

namespace CasinoApp.DataAccess.DB_operations
{
    public class PlayerRepository
    {
        public List<Player> GetAll()
        {
            var players = new List<Player>();

            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Email, Password, Username, Balance, CreatedAt
                FROM Players;
            ";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                players.Add(new Player
                {
                    Id = reader.GetInt32(0),
                    Email = reader.GetString(1),
                    Password = reader.GetString(2),
                    Username = reader.GetString(3),
                    Balance = reader.GetDouble(4),
                    CreatedAt = reader.GetString(5)
                });
            }

            return players;
        }

        public Player? GetById(int id)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Email, Password, Username, Balance, CreatedAt
                FROM Players
                WHERE Id = $id;
            ";
            command.Parameters.AddWithValue("$id", id);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Player
            {
                Id = reader.GetInt32(0),
                Email = reader.GetString(1),
                Password = reader.GetString(2),
                Username = reader.GetString(3),
                Balance = reader.GetDouble(4),
                CreatedAt = reader.GetString(5)
            };
        }

        public Player? GetByEmail(string email)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Email, Password, Username, Balance, CreatedAt
                FROM Players
                WHERE Email = $email;
            ";
            command.Parameters.AddWithValue("$email", email);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Player
            {
                Id = reader.GetInt32(0),
                Email = reader.GetString(1),
                Password = reader.GetString(2),
                Username = reader.GetString(3),
                Balance = reader.GetDouble(4),
                CreatedAt = reader.GetString(5)
            };
        }

        public Player? GetByUsername(string username)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Email, Password, Username, Balance, CreatedAt
                FROM Players
                WHERE Username = $username;
            ";
            command.Parameters.AddWithValue("$username", username);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Player
            {
                Id = reader.GetInt32(0),
                Email = reader.GetString(1),
                Password = reader.GetString(2),
                Username = reader.GetString(3),
                Balance = reader.GetDouble(4),
                CreatedAt = reader.GetString(5)
            };
        }

        public void Create(Player player)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Players (Email, Password, Username, Balance)
                VALUES ($email, $password, $username, $balance);
            ";

            command.Parameters.AddWithValue("$email", player.Email);
            command.Parameters.AddWithValue("$password", player.Password);
            command.Parameters.AddWithValue("$username", player.Username);
            command.Parameters.AddWithValue("$balance", player.Balance);

            command.ExecuteNonQuery();
        }

        public void Update(Player player)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Players
                SET Email = $email,
                    Password = $password,
                    Username = $username,
                    Balance = $balance
                WHERE Id = $id;
            ";

            command.Parameters.AddWithValue("$id", player.Id);
            command.Parameters.AddWithValue("$email", player.Email);
            command.Parameters.AddWithValue("$password", player.Password);
            command.Parameters.AddWithValue("$username", player.Username);
            command.Parameters.AddWithValue("$balance", player.Balance);

            command.ExecuteNonQuery();
        }

        public void UpdateBalance(int playerId, double newBalance)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Players
                SET Balance = $balance
                WHERE Id = $playerId;
            ";

            command.Parameters.AddWithValue("$balance", newBalance);
            command.Parameters.AddWithValue("$playerId", playerId);

            command.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM Players
                WHERE Id = $id;
            ";
            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();
        }
    }
}