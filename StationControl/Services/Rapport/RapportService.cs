using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using StationControl.Models.Rapport;

public class RapportService
{
    public List<RapportMensuelStation> GetRapportMensuel(MySqlConnection connection, int mois, int annee, string stationNom = null)
    {
        var result = new List<RapportMensuelStation>();

        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();

        var query = @"
            SELECT *
            FROM rapport_mensuel_station
            WHERE MONTH(date_planifiee_debut) = @mois
            AND YEAR(date_planifiee_debut) = @annee";

        if (!string.IsNullOrEmpty(stationNom))
        {
            query += " AND station_nom LIKE @stationNom";
        }

        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@mois", mois);
            command.Parameters.AddWithValue("@annee", annee);

            if (!string.IsNullOrEmpty(stationNom))
                command.Parameters.AddWithValue("@stationNom", $"%{stationNom}%");

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var rapport = new RapportMensuelStation
                    {
                        StationNom = reader["station_nom"]?.ToString(),
                        Latitude = reader["latitude"] != DBNull.Value ? Convert.ToDecimal(reader["latitude"]) : (decimal?)null,
                        Longitude = reader["longitude"] != DBNull.Value ? Convert.ToDecimal(reader["longitude"]) : (decimal?)null,
                        TypeStation = reader["type_station"]?.ToString(),
                        Region = reader["region"]?.ToString(),
                        DatePlanifieeDebut = reader["date_planifiee_debut"] as DateTime?,
                        DatePlanifieeFin = reader["date_planifiee_fin"] as DateTime?,
                        DateEffectiveDebut = reader["date_effective_debut"] as DateTime?,
                        DateEffectiveFin = reader["date_effective_fin"] as DateTime?,
                        StatutIntervention = reader["statut_intervention"]?.ToString(),
                        TechnicienPlanifie = reader["technicien_planifie"]?.ToString(),
                        TechnicienEffectif = reader["technicien_effectif"]?.ToString(),
                        BesoinDescription = reader["description_probleme"]?.ToString(),
                        EquipementBesoinLibelle = reader["equipement_besoin_libelle"]?.ToString(),
                        EquipementNumSerie = reader["equipement_num_serie"]?.ToString(),
                        EquipementLibelle = reader["equipement_libelle"]?.ToString(),
                        EquipementStatut = reader["equipement_statut"]?.ToString(),
                        EstAlimentation = reader["est_alimentation"] != DBNull.Value && (bool)reader["est_alimentation"]
                    };

                    result.Add(rapport);
                }
            }
        }

        return result;
    }

}


