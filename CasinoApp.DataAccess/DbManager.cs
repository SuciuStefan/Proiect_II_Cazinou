using Microsoft.Data.Sqlite;

namespace CasinoApp.DataAccess
{
    public static class DbManager
    {
        private const string ConnectionString = "Data Source=casino.db";

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }
    }
}