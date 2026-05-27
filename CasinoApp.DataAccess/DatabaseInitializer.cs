using Microsoft.Data.Sqlite;

namespace CasinoApp.DataAccess
{
    public static class DatabaseInitializer
    {
        public static void Initialize()
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                PRAGMA foreign_keys = ON;

                CREATE TABLE IF NOT EXISTS Players (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Email TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    Username TEXT NOT NULL UNIQUE,
                    Balance REAL NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS Games (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    Type TEXT NOT NULL,
                    MinBet REAL NOT NULL DEFAULT 1,
                    MaxBet REAL NOT NULL DEFAULT 1000,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS Bets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayerId INTEGER NOT NULL,
                    GameId INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    BetTime TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    Status TEXT NOT NULL DEFAULT 'Pending',
                    FOREIGN KEY (PlayerId) REFERENCES Players(Id) ON DELETE CASCADE,
                    FOREIGN KEY (GameId) REFERENCES Games(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Transactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayerId INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    Type TEXT NOT NULL,
                    Description TEXT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (PlayerId) REFERENCES Players(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS PasswordResetTokens (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayerId INTEGER NOT NULL,
                    Token TEXT NOT NULL UNIQUE,
                    ExpiresAt TEXT NOT NULL,
                    IsUsed INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (PlayerId) REFERENCES Players(Id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS IX_Bets_PlayerId ON Bets(PlayerId);
                CREATE INDEX IF NOT EXISTS IX_Bets_GameId ON Bets(GameId);

                CREATE INDEX IF NOT EXISTS IX_Transactions_PlayerId ON Transactions(PlayerId);
            ";

            command.ExecuteNonQuery();

            NormalizeBetsTable(connection);
            SeedGames(connection);
            Console.WriteLine("Database initialized successfully.");
        }

        private static void NormalizeBetsTable(SqliteConnection connection)
        {
            using var inspect = connection.CreateCommand();
            inspect.CommandText = @"
                SELECT sql
                FROM sqlite_master
                WHERE type = 'table' AND name = 'Bets';
            ";

            var sql = inspect.ExecuteScalar()?.ToString() ?? string.Empty;

            if (!sql.Contains("GameSessions", StringComparison.OrdinalIgnoreCase) &&
                !sql.Contains("SessionId", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            using var migrate = connection.CreateCommand();
            migrate.CommandText = @"
                PRAGMA foreign_keys = OFF;

                DROP INDEX IF EXISTS IX_Bets_SessionId;

                CREATE TABLE IF NOT EXISTS Bets_New (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayerId INTEGER NOT NULL,
                    GameId INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    BetTime TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    Status TEXT NOT NULL DEFAULT 'Pending',
                    FOREIGN KEY (PlayerId) REFERENCES Players(Id) ON DELETE CASCADE,
                    FOREIGN KEY (GameId) REFERENCES Games(Id) ON DELETE CASCADE
                );

                INSERT OR IGNORE INTO Bets_New (Id, PlayerId, GameId, Amount, BetTime, Status)
                SELECT Id, PlayerId, GameId, Amount, BetTime, Status
                FROM Bets;

                DROP TABLE Bets;
                ALTER TABLE Bets_New RENAME TO Bets;

                CREATE INDEX IF NOT EXISTS IX_Bets_PlayerId ON Bets(PlayerId);
                CREATE INDEX IF NOT EXISTS IX_Bets_GameId ON Bets(GameId);

                PRAGMA foreign_keys = ON;
            ";

            migrate.ExecuteNonQuery();
        }

        private static void SeedGames(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();

            command.CommandText = @"
        INSERT OR IGNORE INTO Games (Name, Type, MinBet, MaxBet, IsActive) VALUES
        ('Blackjack', 'Cards', 5, 1000, 1),
        ('Slots', 'Slots', 1, 500, 1),
        ('Craps', 'Dice', 5, 1000, 1),
        ('Mines', 'Arcade', 1, 500, 1),
        ('Flip a Coin', 'Arcade', 1, 300, 1),
        ('Wheel of Fortune', 'Wheel', 1, 500, 1),
        ('Scratch Cards', 'Scratch', 5, 500, 1),
        ('Roulette', 'Table', 2, 750, 1);
    ";

            command.ExecuteNonQuery();
        }
    }
}
