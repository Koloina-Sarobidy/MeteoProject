using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using StationControl.Models.Intervention;
using StationControl.Services.Besoin;

namespace StationControl.Services.Intervention
{
    public static class InterventionService
    {
        public static List<int> PlanifierInterventions(
            MySqlConnection connection,
            List<Models.Intervention.Intervention> interventions
        )
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (interventions == null || interventions.Count == 0)
                throw new ArgumentException("La liste d'interventions est vide.");

            var insertedIds = new List<int>();
            var besoinsIds = new List<int>();

            string query = @"
                INSERT INTO intervention (
                    station_id,
                    date,
                    besoin_station_id,
                    utilisateur_planification_id,
                    date_planifiee_debut,
                    date_planifiee_fin,
                    technicien_planifie,
                    statut
                ) VALUES (
                    @station_id,
                    @date,
                    @besoin_station_id,
                    @utilisateur_planification_id,
                    @date_planifiee_debut,
                    @date_planifiee_fin,
                    @technicien_planifie,
                    @statut
                );
                SELECT LAST_INSERT_ID();
            ";

            foreach (var inter in interventions)
            {
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@station_id", inter.StationId);
                cmd.Parameters.AddWithValue("@date", inter.Date ?? DateTime.Now.Date);
                cmd.Parameters.AddWithValue("@besoin_station_id", (object?)inter.BesoinStationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@utilisateur_planification_id", (object?)inter.UtilisateurPlanificationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@date_planifiee_debut", inter.DatePlanifieeDebut);
                cmd.Parameters.AddWithValue("@date_planifiee_fin", (object?)inter.DatePlanifieeFin ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@technicien_planifie", inter.TechnicienPlanifie);
                cmd.Parameters.AddWithValue("@statut", inter.Statut ?? "Planifiée");

                object result = cmd.ExecuteScalar();
                insertedIds.Add(Convert.ToInt32(result));

                if (inter.BesoinStationId.HasValue)
                    besoinsIds.Add(inter.BesoinStationId.Value);
            }

            if (besoinsIds.Count > 0)
            {
                BesoinService.TraiterBesoin(connection, besoinsIds);
            }

            return insertedIds;
        }

        public static Models.Intervention.Intervention GetInterventionByBesoinId(
            MySqlConnection connection,
            int besoinStationId
        )
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT id, station_id, date, besoin_station_id, utilisateur_planification_id,
                       utilisateur_effectif_id, date_planifiee_debut, date_planifiee_fin,
                       date_effective_debut, date_effective_fin, technicien_planifie, technicien_effectif, statut
                FROM intervention
                WHERE besoin_station_id = @besoin_station_id
                LIMIT 1;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@besoin_station_id", besoinStationId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapIntervention(reader);
            }

            return null;
        }

        public static List<Models.Intervention.Intervention> GetInterventions(
            MySqlConnection connection,
            int? stationId = null,
            string statut = null,
            int? mois = null,
            int? annee = null
        )
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var interventions = new List<Models.Intervention.Intervention>();

            if (!mois.HasValue || !annee.HasValue)
            {
                DateTime now = DateTime.Now;
                mois ??= now.Month;
                annee ??= now.Year;
            }

            DateTime dateDebut = new DateTime(annee.Value, mois.Value, 1);
            DateTime dateFin = dateDebut.AddMonths(1).AddDays(-1);

            string query = @"
                SELECT id, station_id, date, besoin_station_id, utilisateur_planification_id,
                       utilisateur_effectif_id, date_planifiee_debut, date_planifiee_fin,
                       date_effective_debut, date_effective_fin, technicien_planifie, technicien_effectif, statut
                FROM intervention
                WHERE date_planifiee_debut >= @dateDebut AND date_planifiee_debut <= @dateFin
            ";

            if (stationId.HasValue)
                query += " AND station_id = @stationId";

            if (!string.IsNullOrEmpty(statut))
                query += " AND statut = @statut";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@dateDebut", dateDebut);
            cmd.Parameters.AddWithValue("@dateFin", dateFin);

            if (stationId.HasValue)
                cmd.Parameters.AddWithValue("@stationId", stationId.Value);
            if (!string.IsNullOrEmpty(statut))
                cmd.Parameters.AddWithValue("@statut", statut);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                interventions.Add(MapIntervention(reader));
            }

            return interventions;
        }

        private static Models.Intervention.Intervention MapIntervention(MySqlDataReader reader)
        {
            return new Models.Intervention.Intervention
            {
                Id = reader.GetInt32("id"),
                StationId = reader.GetInt32("station_id"),
                Date = reader.IsDBNull(reader.GetOrdinal("date")) ? (DateTime?)null : reader.GetDateTime("date"),
                BesoinStationId = reader.IsDBNull(reader.GetOrdinal("besoin_station_id")) ? null : reader.GetInt32("besoin_station_id"),
                UtilisateurPlanificationId = reader.IsDBNull(reader.GetOrdinal("utilisateur_planification_id")) ? null : reader.GetInt32("utilisateur_planification_id"),
                UtilisateurEffectifId = reader.IsDBNull(reader.GetOrdinal("utilisateur_effectif_id")) ? null : reader.GetInt32("utilisateur_effectif_id"),
                DatePlanifieeDebut = reader.GetDateTime("date_planifiee_debut"),
                DatePlanifieeFin = reader.IsDBNull(reader.GetOrdinal("date_planifiee_fin")) ? (DateTime?)null : reader.GetDateTime("date_planifiee_fin"),
                DateEffectiveDebut = reader.IsDBNull(reader.GetOrdinal("date_effective_debut")) ? (DateTime?)null : reader.GetDateTime("date_effective_debut"),
                DateEffectiveFin = reader.IsDBNull(reader.GetOrdinal("date_effective_fin")) ? (DateTime?)null : reader.GetDateTime("date_effective_fin"),
                TechnicienPlanifie = reader.IsDBNull(reader.GetOrdinal("technicien_planifie")) ? null : reader.GetString("technicien_planifie"),
                TechnicienEffectif = reader.IsDBNull(reader.GetOrdinal("technicien_effectif")) ? null : reader.GetString("technicien_effectif"),
                Statut = reader.IsDBNull(reader.GetOrdinal("statut")) ? null : reader.GetString("statut")
            };
        }

        public static void AnnulerIntervention(MySqlConnection connection, int interventionId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (interventionId <= 0)
                throw new ArgumentException("ID de l'intervention invalide.", nameof(interventionId));

            using var transaction = connection.BeginTransaction();

            try
            {
                int? besoinId = null;

                string selectQuery = "SELECT besoin_station_id FROM intervention WHERE id = @id LIMIT 1;";
                using (var selectCmd = new MySqlCommand(selectQuery, connection, transaction))
                {
                    selectCmd.Parameters.AddWithValue("@id", interventionId);
                    var result = selectCmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        besoinId = Convert.ToInt32(result);
                }

                if (besoinId.HasValue)
                {
                    string deleteQuery = "DELETE FROM intervention WHERE id = @id;";
                    using var deleteCmd = new MySqlCommand(deleteQuery, connection, transaction);
                    deleteCmd.Parameters.AddWithValue("@id", interventionId);
                    deleteCmd.ExecuteNonQuery();

                    string updateBesoinQuery = "UPDATE besoin_station SET est_traite = 0 WHERE id = @besoinId;";
                    using var updateCmd = new MySqlCommand(updateBesoinQuery, connection, transaction);
                    updateCmd.Parameters.AddWithValue("@besoinId", besoinId.Value);
                    updateCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public static void EffectuerIntervention(
            MySqlConnection connection,
            int interventionId,
            DateTime dateEffectiveDebut,
            DateTime? dateEffectiveFin,
            string technicienEffectif,
            int? utilisateurEffectifId = null
        )
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (interventionId <= 0)
                throw new ArgumentException("ID de l'intervention invalide.", nameof(interventionId));

            string statut = dateEffectiveFin.HasValue ? "Terminée" : "En cours";

            using var transaction = connection.BeginTransaction();
            try
            {
                string query = @"
                    UPDATE intervention
                    SET
                        date_effective_debut = @dateEffectiveDebut,
                        date_effective_fin = @dateEffectiveFin,
                        technicien_effectif = @technicienEffectif,
                        utilisateur_effectif_id = @utilisateurEffectifId,
                        statut = @statut
                    WHERE id = @interventionId;
                ";

                using var cmd = new MySqlCommand(query, connection, transaction);
                cmd.Parameters.AddWithValue("@interventionId", interventionId);
                cmd.Parameters.AddWithValue("@dateEffectiveDebut", dateEffectiveDebut);
                cmd.Parameters.AddWithValue("@dateEffectiveFin", (object?)dateEffectiveFin ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@technicienEffectif", technicienEffectif);
                cmd.Parameters.AddWithValue("@utilisateurEffectifId", (object?)utilisateurEffectifId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@statut", statut);
                cmd.ExecuteNonQuery();

                if (dateEffectiveFin.HasValue)
                {
                    MettreAJourEquipementSiRemplacement(connection, transaction, interventionId);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public static void TerminerIntervention(
            MySqlConnection connection,
            int interventionId,
            DateTime dateEffectiveFin
        )
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (interventionId <= 0)
                throw new ArgumentException("ID de l'intervention invalide.", nameof(interventionId));

            using var transaction = connection.BeginTransaction();
            try
            {
                
            string query = @"
                UPDATE intervention
                SET
                    date_effective_fin = @dateEffectiveFin,
                    statut = 'Terminée'
                WHERE id = @interventionId;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@interventionId", interventionId);
            cmd.Parameters.AddWithValue("@dateEffectiveFin", dateEffectiveFin);

            cmd.ExecuteNonQuery();

            MettreAJourEquipementSiRemplacement(connection, transaction, interventionId);
            transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            
        }

        private static void MettreAJourEquipementSiRemplacement(MySqlConnection connection, MySqlTransaction transaction, int interventionId)
        {
            string selectQuery = @"
                SELECT bs.id AS besoinId,
                       bs.equipement_station_id AS equipementId,
                       bs.description_probleme AS probleme,
                       eq.libelle AS typeBesoin
                FROM intervention i
                JOIN besoin_station bs ON bs.id = i.besoin_station_id
                JOIN equipement_besoin eq ON bs.equipement_besoin_id = eq.id
                WHERE i.id = @interventionId
                LIMIT 1;
            ";

            int? besoinId = null;
            int? equipementId = null;
            string typeBesoin = null;

            using (var selectCmd = new MySqlCommand(selectQuery, connection, transaction))
            {
                selectCmd.Parameters.AddWithValue("@interventionId", interventionId);
                using var reader = selectCmd.ExecuteReader();
                if (reader.Read())
                {
                    besoinId = reader.IsDBNull(reader.GetOrdinal("besoinId")) ? null : reader.GetInt32("besoinId");
                    equipementId = reader.IsDBNull(reader.GetOrdinal("equipementId")) ? null : reader.GetInt32("equipementId");
                    typeBesoin = reader.IsDBNull(reader.GetOrdinal("typeBesoin")) ? null : reader.GetString("typeBesoin");
                }
            }

            if (!string.IsNullOrEmpty(typeBesoin) &&
                typeBesoin.ToLower().Contains("remplacement") &&
                equipementId.HasValue)
            {
                string updateEquipement = @"
                    UPDATE equipement_station
                    SET est_remplace = TRUE,
                        statut = 'Non Fonctionnel'
                    WHERE id = @equipementId;
                ";
                using var cmd = new MySqlCommand(updateEquipement, connection, transaction);
                cmd.Parameters.AddWithValue("@equipementId", equipementId.Value);
                cmd.ExecuteNonQuery();
            }

            if (besoinId.HasValue)
            {
                string updateBesoin = "UPDATE besoin_station SET est_traite = TRUE WHERE id = @besoinId;";
                using var cmd = new MySqlCommand(updateBesoin, connection, transaction);
                cmd.Parameters.AddWithValue("@besoinId", besoinId.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public static Dictionary<string, int> GetCurativeInterventionCountByStation(MySqlConnection connection)
        {
            var results = new Dictionary<string, int>();

            string query = @"
                SELECT s.nom AS station_name, COUNT(i.id) AS total
                FROM intervention i
                INNER JOIN station s ON s.id = i.station_id
                WHERE i.statut = 'Terminée'
                AND MONTH(i.date_effective_fin) = MONTH(CURRENT_DATE())
                AND YEAR(i.date_effective_fin) = YEAR(CURRENT_DATE())
                GROUP BY s.nom;
            ";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string stationName = reader.GetString("station_name");
                int total = reader.GetInt32("total");

                results[stationName] = total;
            }

            return results;
        }

        public static bool EstInterventionEnRetard(MySqlConnection connection, int stationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            string query = @"
                SELECT 1
                FROM intervention
                WHERE date_effective_debut IS NULL
                  AND date_planifiee_fin IS NOT NULL
                  AND date_planifiee_fin < @Now
                  AND station_id = @StationId
                LIMIT 1;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@StationId", stationId);
            cmd.Parameters.AddWithValue("@Now", DateTime.Now);

            using var reader = cmd.ExecuteReader();
            return reader.Read();
        }
    }
}
