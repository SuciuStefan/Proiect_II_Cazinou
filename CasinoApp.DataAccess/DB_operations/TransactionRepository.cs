using CasinoApp.DataAccess.Entities;

namespace CasinoApp.DataAccess.DB_operations
{
    public class TransactionRepository
    {
        public List<Transaction> GetAll()
        {
            var transactions = new List<Transaction>();

            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, PlayerId, Amount, Type, Description, CreatedAt
                FROM Transactions;
            ";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                transactions.Add(new Transaction
                {
                    Id = reader.GetInt32(0),
                    PlayerId = reader.GetInt32(1),
                    Amount = reader.GetDouble(2),
                    Type = reader.GetString(3),
                    Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CreatedAt = reader.GetString(5)
                });
            }

            return transactions;
        }

        public List<Transaction> GetByPlayerId(int playerId)
        {
            var transactions = new List<Transaction>();

            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, PlayerId, Amount, Type, Description, CreatedAt
                FROM Transactions
                WHERE PlayerId = $playerId
                ORDER BY CreatedAt DESC;
            ";
            command.Parameters.AddWithValue("$playerId", playerId);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                transactions.Add(new Transaction
                {
                    Id = reader.GetInt32(0),
                    PlayerId = reader.GetInt32(1),
                    Amount = reader.GetDouble(2),
                    Type = reader.GetString(3),
                    Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CreatedAt = reader.GetString(5)
                });
            }

            return transactions;
        }

        public Transaction? GetById(int id)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, PlayerId, Amount, Type, Description, CreatedAt
                FROM Transactions
                WHERE Id = $id;
            ";
            command.Parameters.AddWithValue("$id", id);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Transaction
            {
                Id = reader.GetInt32(0),
                PlayerId = reader.GetInt32(1),
                Amount = reader.GetDouble(2),
                Type = reader.GetString(3),
                Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = reader.GetString(5)
            };
        }

        public void Create(Transaction transaction)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Transactions (PlayerId, Amount, Type, Description)
                VALUES ($playerId, $amount, $type, $description);
            ";

            command.Parameters.AddWithValue("$playerId", transaction.PlayerId);
            command.Parameters.AddWithValue("$amount", transaction.Amount);
            command.Parameters.AddWithValue("$type", transaction.Type);
            command.Parameters.AddWithValue("$description", (object?)transaction.Description ?? DBNull.Value);

            command.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM Transactions
                WHERE Id = $id;
            ";
            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();
        }
    }
}