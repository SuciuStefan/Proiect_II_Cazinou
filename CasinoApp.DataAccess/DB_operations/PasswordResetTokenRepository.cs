using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;

namespace CasinoApp.DataAccess.DB_operations
{
    public class PasswordResetTokenRepository
    {
        public void Create(int playerId, string token, DateTime expiresAt)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO PasswordResetTokens (PlayerId, Token, ExpiresAt)
                VALUES ($playerId, $token, $expiresAt);
            ";

            command.Parameters.AddWithValue("$playerId", playerId);
            command.Parameters.AddWithValue("$token", token);
            command.Parameters.AddWithValue("$expiresAt", expiresAt.ToString("yyyy-MM-dd HH:mm:ss"));

            command.ExecuteNonQuery();
        }

        public (int PlayerId, bool IsValid)? GetValidToken(string token)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT PlayerId
                FROM PasswordResetTokens
                WHERE Token = $token
                  AND IsUsed = 0
                  AND datetime(ExpiresAt) > datetime('now');
            ";

            command.Parameters.AddWithValue("$token", token);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return (reader.GetInt32(0), true);
        }

        public void MarkAsUsed(string token)
        {
            using var connection = DbManager.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE PasswordResetTokens
                SET IsUsed = 1
                WHERE Token = $token;
            ";

            command.Parameters.AddWithValue("$token", token);
            command.ExecuteNonQuery();
        }
    }
}
