using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using StationControl.Models.Equipement;

namespace StationControl.Services.Station
{
    public class AlimentationService
    {
        public static Alimentation GetAlimentationById(MySqlConnection connection, int id)
        {
            Alimentation alimentation = null;

            string query = @"SELECT id, nom, description 
                             FROM alimentation WHERE id = @id";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@id", id);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        alimentation = new Alimentation
                        {
                            Id = reader.GetInt32("id"),
                            Libelle = reader.GetString("nom"),
                            Description = reader.GetString("description")
                        };
                    }
                }
            }
            return alimentation;
        }
        public static void InsertAlimentation(MySqlConnection connection, Alimentation alimentation)
        {
            string query = @"INSERT INTO alimentation (nom, description) 
                             VALUES (@libelle, @description)";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@libelle", alimentation.Libelle);
                cmd.Parameters.AddWithValue("@description", alimentation.Description);

                object result = cmd.ExecuteScalar();
                alimentation.Id = Convert.ToInt32(result);
            }
        }
        public static List<Alimentation> GetAllAlimentation(MySqlConnection connection)
        {
            List<Alimentation> alimentations = new List<Alimentation>();

            string query = @"SELECT id, nom, description 
                             FROM alimentation;";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Alimentation alimentation = new Alimentation
                        {
                            Id = reader.GetInt32("id"),
                            Libelle = reader.GetString("nom"),
                            Description = reader.GetString("description")
                        };
                        alimentations.Add(alimentation);
                    }
                }
            }
            return alimentations;
        }
    }
}
