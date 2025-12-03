using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using StationControl.Models.Equipement;

namespace StationControl.Services.Station
{
    public class CapteurService
    {
        public static Capteur GetCapteurById(MySqlConnection connection, int id)
        {
            Capteur capteur = null;

            string query = @"SELECT id, nom, parametre 
                             FROM capteur WHERE id = @id";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@id", id);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        capteur = new Capteur
                        {
                            Id = reader.GetInt32("id"),
                            Libelle = reader.GetString("nom"),
                            Parametre = reader.GetString("parametre")
                        };
                    }
                }
            }
            return capteur;
        }
        public static void InsertCapteur(MySqlConnection connection, Capteur capteur)
        {
            string query = @"INSERT INTO capteur (nom, parametre) 
                             VALUES (@libelle, @parametre)";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@libelle", capteur.Libelle);
                cmd.Parameters.AddWithValue("@parametre", capteur.Parametre);

                object result = cmd.ExecuteScalar();
                capteur.Id = Convert.ToInt32(result);
            }
        }
        public static List<Capteur> GetAllCapteur(MySqlConnection connection)
        {
            List<Capteur> capteurs = new List<Capteur>();

            string query = @"SELECT id, nom, parametre 
                             FROM capteur";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Capteur capteur = new Capteur
                        {
                            Id = reader.GetInt32("id"),
                            Libelle = reader.GetString("nom"),
                            Parametre = reader.GetString("parametre")
                        };
                        capteurs.Add(capteur);
                    }
                }
            }
            return capteurs;
        }
    }
}
