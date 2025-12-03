using System;
using MySql.Data.MySqlClient;
using StationControl.Models.Station;

namespace StationControl.Services.Station
{
    public class EquipementStationService
    {
        public static EquipementStation GetEquipementStationById(MySqlConnection connection, int id)
        {
            EquipementStation equipementStation = null;
            int? importanceId = null; 

            string query = @"
                SELECT 
                    id,
                    station_id,
                    capteur_id,
                    alimentation_id,
                    importance_id,
                    num_serie,
                    date_debut,
                    date_fin,
                    estimation_vie_annee,
                    est_alimentation,
                    statut,
                FROM equipement_station
                WHERE id = @id;";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@id", id);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        equipementStation = new EquipementStation
                        {
                            Id = reader.GetInt32("id"),
                            NumSerie = reader.GetString("num_serie"),
                            Statut = reader.GetString("statut"),
                            CapteurId = reader["capteur_id"] != DBNull.Value ? reader.GetInt32("capteur_id") : null,
                            AlimentationId = reader["alimentation_id"] != DBNull.Value ? reader.GetInt32("alimentation_id") : null,
                            EstAlimentation = reader.GetBoolean("est_alimentation"),
                            DateDebut = reader.GetDateTime("date_debut"),
                            DateFin = reader["date_fin"] != DBNull.Value ? reader.GetDateTime("date_fin") : null,
                            EstimationCout = reader["estimation_vie_annee"] != DBNull.Value ? reader.GetDecimal("estimation_vie_annee") : null,
                        };

                    }
                }
            }
            return equipementStation;
        }

        public static bool UpdateStatutEquipementStation(MySqlConnection connection, int id, string nouveauStatut)
        {
            string query = @"
                UPDATE equipement_station
                SET statut = @statut
                WHERE id = @id;
            ";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@statut", nouveauStatut);
                cmd.Parameters.AddWithValue("@id", id);

                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

    }
}
