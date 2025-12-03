using System.Security.Cryptography;
using MySql.Data.MySqlClient;

namespace StationControl.Services.Auth
{
    public static class GenerateurCode
    {
        public static string GenerateResetCode()
        {
            var bytes = RandomNumberGenerator.GetBytes(4);
            int code = BitConverter.ToInt32(bytes, 0) & 0x7fffffff;
            return (code % 1000000).ToString("D6");
        }

        public static void SaveResetCode(MySqlConnection connection, int utilisateurId, string code)
        {
            string query = @"
                INSERT INTO code_mot_de_passe(utilisateur_id, code, date_expiration, est_utilise)
                VALUES (@utilisateur_id, @code, @date_expiration, 0);";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@utilisateur_id", utilisateurId);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@date_expiration", DateTime.Now.AddMinutes(15));
            cmd.ExecuteNonQuery();
        }

        public static bool ValidateCode(MySqlConnection connection, int utilisateurId, string code)
        {
            string query = @"
                SELECT Id, code, date_expiration
                    FROM code_mot_de_passe
                    WHERE utilisateur_id = @utilisateur_id and code = @code
                    AND est_utilise = 0
                    AND date_expiration >= NOW()
                    ORDER BY id DESC
                    LIMIT 1;
                    ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@utilisateur_id", utilisateurId);
            cmd.Parameters.AddWithValue("@code", code);

            var result = cmd.ExecuteScalar();
            if (result == null) return false;

            string updateQuery = "UPDATE code_mot_de_passe SET est_utilise = 1 WHERE Id = @id;";
            using var updateCmd = new MySqlCommand(updateQuery, connection);
            updateCmd.Parameters.AddWithValue("@id", Convert.ToInt32(result));
            updateCmd.ExecuteNonQuery();

            return true;
        }
    }
}
