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

                CREATE TABLE IF NOT EXISTS GameSessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayerId INTEGER NOT NULL,
                    GameId INTEGER NOT NULL,
                    StartedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    EndedAt TEXT NULL,
                    Status TEXT NOT NULL DEFAULT 'Active',
                    FOREIGN KEY (PlayerId) REFERENCES Players(Id) ON DELETE CASCADE,
                    FOREIGN KEY (GameId) REFERENCES Games(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Bets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayerId INTEGER NOT NULL,
                    GameId INTEGER NOT NULL,
                    SessionId INTEGER NULL,
                    Amount REAL NOT NULL,
                    BetTime TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    Status TEXT NOT NULL DEFAULT 'Pending',
                    FOREIGN KEY (PlayerId) REFERENCES Players(Id) ON DELETE CASCADE,
                    FOREIGN KEY (GameId) REFERENCES Games(Id) ON DELETE CASCADE,
                    FOREIGN KEY (SessionId) REFERENCES GameSessions(Id) ON DELETE SET NULL
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

                CREATE TABLE IF NOT EXISTS GameResults (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BetId INTEGER NOT NULL,
                    SessionId INTEGER NULL,
                    PlayerId INTEGER NOT NULL,
                    GameId INTEGER NOT NULL,
                    ResultType TEXT NOT NULL,
                    Multiplier REAL NOT NULL DEFAULT 0,
                    WinAmount REAL NOT NULL DEFAULT 0,
                    ResultData TEXT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (BetId) REFERENCES Bets(Id) ON DELETE CASCADE,
                    FOREIGN KEY (SessionId) REFERENCES GameSessions(Id) ON DELETE SET NULL,
                    FOREIGN KEY (PlayerId) REFERENCES Players(Id) ON DELETE CASCADE,
                    FOREIGN KEY (GameId) REFERENCES Games(Id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS IX_GameSessions_PlayerId ON GameSessions(PlayerId);
                CREATE INDEX IF NOT EXISTS IX_GameSessions_GameId ON GameSessions(GameId);

                CREATE INDEX IF NOT EXISTS IX_Bets_PlayerId ON Bets(PlayerId);
                CREATE INDEX IF NOT EXISTS IX_Bets_GameId ON Bets(GameId);
                CREATE INDEX IF NOT EXISTS IX_Bets_SessionId ON Bets(SessionId);

                CREATE INDEX IF NOT EXISTS IX_Transactions_PlayerId ON Transactions(PlayerId);

                CREATE INDEX IF NOT EXISTS IX_GameResults_BetId ON GameResults(BetId);
                CREATE INDEX IF NOT EXISTS IX_GameResults_PlayerId ON GameResults(PlayerId);
                CREATE INDEX IF NOT EXISTS IX_GameResults_GameId ON GameResults(GameId);
                CREATE INDEX IF NOT EXISTS IX_GameResults_SessionId ON GameResults(SessionId);
            ";

            command.ExecuteNonQuery();

            SeedGames(connection);
            Console.WriteLine("Database initialized successfully.");
        }

        private static void SeedGames(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT OR IGNORE INTO Games (Name, Type, MinBet, MaxBet, IsActive) VALUES
                ('Slots Classic', 'Slots', 1, 500, 1),
                ('Blackjack', 'Cards', 5, 1000, 1),
                ('Roulette', 'Table', 2, 750, 1),
                ('Poker Video', 'Poker', 1, 300, 1);
            ";

            command.ExecuteNonQuery();
        }
    }
}