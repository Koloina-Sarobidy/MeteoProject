using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using StationControl.Models.Station;

namespace StationControl.Services.Station
{
    public class TechnicienService
    {
        public static Models.Technicien.Technicien? GetTechnicienById(MySqlConnection connection, int id)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var query = "SELECT * FROM technicien WHERE id = @Id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Models.Technicien.Technicien
                {
                    Id = reader.GetInt32("id"),
                    Nom = reader.GetString("nom"),
                    Prenom = reader.GetString("prenom"),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                        ? null
                        : reader.GetString("description")
                };
            }
            return null;
        }

        public static void InsertTechnicien(MySqlConnection connection, Models.Technicien.Technicien technicien)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"INSERT INTO technicien (nom, prenom, description)
                             VALUES (@nom, @prenom, @description);";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@nom", technicien.Nom);
            cmd.Parameters.AddWithValue("@prenom", technicien.Prenom);
            cmd.Parameters.AddWithValue("@description", technicien.Description ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public static List<Models.Technicien.Technicien> GetAllTechniciens(MySqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            List<Models.Technicien.Technicien> techniciens = new();

            string query = @"SELECT id, nom, prenom, description FROM technicien ORDER BY id;";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var t = new Models.Technicien.Technicien
                {
                    Id = reader.GetInt32("id"),
                    Nom = reader.GetString("nom"),
                    Prenom = reader.GetString("prenom"),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                        ? null
                        : reader.GetString("description")
                };
                techniciens.Add(t);
            }

            return techniciens;
        }
    }
}
