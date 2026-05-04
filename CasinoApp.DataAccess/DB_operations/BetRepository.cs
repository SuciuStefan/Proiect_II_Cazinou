using CasinoApp.DataAccess.Entities;

namespace CasinoApp.DataAccess.DB_operations
{
    public class BetRepository
    {
        public List<Bet> GetAll()
        {
            var bets = new List<Bet>();

            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, PlayerId, GameId, SessionId, Amount, BetTime, Status
                FROM Bets;
            ";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                bets.Add(new Bet
                {
                    Id = reader.GetInt32(0),
                    PlayerId = reader.GetInt32(1),
                    GameId = reader.GetInt32(2),
                    SessionId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Amount = reader.GetDouble(4),
                    BetTime = reader.GetString(5),
                    Status = reader.GetString(6)
                });
            }

            return bets;
        }

        public Bet? GetById(int id)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, PlayerId, GameId, SessionId, Amount, BetTime, Status
                FROM Bets
                WHERE Id = $id;
            ";
            command.Parameters.AddWithValue("$id", id);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Bet
            {
                Id = reader.GetInt32(0),
                PlayerId = reader.GetInt32(1),
                GameId = reader.GetInt32(2),
                SessionId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Amount = reader.GetDouble(4),
                BetTime = reader.GetString(5),
                Status = reader.GetString(6)
            };
        }

        public List<Bet> GetByPlayerId(int playerId)
        {
            var bets = new List<Bet>();

            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, PlayerId, GameId, SessionId, Amount, BetTime, Status
                FROM Bets
                WHERE PlayerId = $playerId
                ORDER BY BetTime DESC;
            ";
            command.Parameters.AddWithValue("$playerId", playerId);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                bets.Add(new Bet
                {
                    Id = reader.GetInt32(0),
                    PlayerId = reader.GetInt32(1),
                    GameId = reader.GetInt32(2),
                    SessionId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Amount = reader.GetDouble(4),
                    BetTime = reader.GetString(5),
                    Status = reader.GetString(6)
                });
            }

            return bets;
        }

        public List<Bet> GetBySessionId(int sessionId)
        {
            var bets = new List<Bet>();

            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, PlayerId, GameId, SessionId, Amount, BetTime, Status
                FROM Bets
                WHERE SessionId = $sessionId
                ORDER BY BetTime DESC;
            ";
            command.Parameters.AddWithValue("$sessionId", sessionId);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                bets.Add(new Bet
                {
                    Id = reader.GetInt32(0),
                    PlayerId = reader.GetInt32(1),
                    GameId = reader.GetInt32(2),
                    SessionId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Amount = reader.GetDouble(4),
                    BetTime = reader.GetString(5),
                    Status = reader.GetString(6)
                });
            }

            return bets;
        }

        public void Create(Bet bet)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Bets (PlayerId, GameId, SessionId, Amount, Status)
                VALUES ($playerId, $gameId, $sessionId, $amount, $status);
            ";

            command.Parameters.AddWithValue("$playerId", bet.PlayerId);
            command.Parameters.AddWithValue("$gameId", bet.GameId);
            command.Parameters.AddWithValue("$sessionId", (object?)bet.SessionId ?? DBNull.Value);
            command.Parameters.AddWithValue("$amount", bet.Amount);
            command.Parameters.AddWithValue("$status", bet.Status);

            command.ExecuteNonQuery();
        }

        public void UpdateStatus(int betId, string status)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Bets
                SET Status = $status
                WHERE Id = $betId;
            ";

            command.Parameters.AddWithValue("$status", status);
            command.Parameters.AddWithValue("$betId", betId);

            command.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM Bets
                WHERE Id = $id;
            ";
            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();
        }
    }
}