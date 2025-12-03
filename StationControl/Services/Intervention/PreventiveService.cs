using System;
using MySql.Data.MySqlClient;
using StationControl.Models.Intervention;
using StationControl.Services.Auth;
using StationControl.Services.Station;

namespace StationControl.Services.Intervention
{
    public static class PreventiveService
    {
        public static void InsertPreventive(MySqlConnection connection, Preventive preventive)
        {
            if (preventive == null)
                throw new ArgumentNullException(nameof(preventive));
            if (preventive.UtilisateurPlanificateur == null)
                throw new ArgumentNullException(nameof(preventive.UtilisateurPlanificateur));
            if (preventive.Station == null)
                throw new ArgumentNullException(nameof(preventive.Station));

            string query = @"
                INSERT INTO preventive 
                (utilisateur_planificateur_id, utilisateur_effectif_id, station_id,
                 date_prevue_debut, date_prevue_fin,
                 date_effective_debut, date_effective_fin,
                 est_complete, est_annule,
                 description)
                VALUES 
                (@planificateurId, @effectifId, @stationId,
                 @datePrevueDebut, @datePrevueFin,
                 @dateEffectiveDebut, @dateEffectiveFin,
                 @estComplete, @estAnnule,
                 @description);";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@planificateurId", preventive.UtilisateurPlanificateur.Id);
            command.Parameters.AddWithValue("@effectifId", preventive.UtilisateurEffectif?.Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@stationId", preventive.Station.Id);
            command.Parameters.AddWithValue("@datePrevueDebut", preventive.DatePrevueDebut);
            command.Parameters.AddWithValue("@datePrevueFin", preventive.DatePrevueFin ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@dateEffectiveDebut", preventive.DateEffectiveDebut ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@dateEffectiveFin", preventive.DateEffectiveFin ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@estComplete", preventive.EstComplete);
            command.Parameters.AddWithValue("@estAnnule", preventive.EstAnnule);
            command.Parameters.AddWithValue("@description", preventive.Description ?? "");

            command.ExecuteNonQuery();
            preventive.Id = (int)command.LastInsertedId;
        }

        public static List<Preventive> GetAllPreventive(MySqlConnection connection)
        {
            var preventives = new List<Preventive>();

            string query = @"
                SELECT p.id, p.date_prevue_debut, p.date_prevue_fin,
                       p.date_effective_debut, p.date_effective_fin,
                       p.est_complete, p.est_annule, p.description,
                       p.utilisateur_planificateur_id, p.utilisateur_effectif_id,
                       p.station_id
                FROM preventive p
                ORDER BY p.id DESC;";

            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            var tempList = new List<(Preventive preventive, int? planId, int? effId, int stationId)>();

            while (reader.Read())
            {
                var preventive = new Preventive
                {
                    Id = reader.GetInt32("id"),
                    Description = reader.GetString("description"),
                    EstComplete = reader.GetBoolean("est_complete"),
                    EstAnnule = reader.GetBoolean("est_annule"),
                    DatePrevueDebut = reader.GetDateTime("date_prevue_debut"),
                    DatePrevueFin = reader.IsDBNull(reader.GetOrdinal("date_prevue_fin")) ? (DateTime?)null : reader.GetDateTime("date_prevue_fin"),
                    DateEffectiveDebut = reader.IsDBNull(reader.GetOrdinal("date_effective_debut")) ? (DateTime?)null : reader.GetDateTime("date_effective_debut"),
                    DateEffectiveFin = reader.IsDBNull(reader.GetOrdinal("date_effective_fin")) ? (DateTime?)null : reader.GetDateTime("date_effective_fin"),
                };

                int? planId = reader.IsDBNull(reader.GetOrdinal("utilisateur_planificateur_id")) ? (int?)null : reader.GetInt32("utilisateur_planificateur_id");
                int? effId = reader.IsDBNull(reader.GetOrdinal("utilisateur_effectif_id")) ? (int?)null : reader.GetInt32("utilisateur_effectif_id");
                int stationId = reader.GetInt32("station_id");

                tempList.Add((preventive, planId, effId, stationId));
            }

            foreach (var (preventive, planId, effId, stationId) in tempList)
            {
                if (planId.HasValue)
                    preventive.UtilisateurPlanificateur = UtilisateurService.GetUtilisateurById(connection, planId.Value);

                if (effId.HasValue)
                    preventive.UtilisateurEffectif = UtilisateurService.GetUtilisateurById(connection, effId.Value);

                preventive.Station = StationService.GetStationById(connection, stationId);

                preventives.Add(preventive);
            }

            return preventives;
        }
        public static List<Preventive> GetPreventiveByStation(
            MySqlConnection connection,
            int stationId,
            DateTime? dateDebut = null,
            DateTime? dateFin = null)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var preventives = new List<Preventive>();

            DateTime debutPeriode = dateDebut ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime finPeriode = dateFin ?? debutPeriode.AddMonths(1).AddDays(-1);

            string query = @"
                SELECT p.id, p.date_prevue_debut, p.date_prevue_fin,
                    p.date_effective_debut, p.date_effective_fin,
                    p.est_complete, p.est_annule, p.description,
                    p.utilisateur_planificateur_id, p.utilisateur_effectif_id,
                    p.station_id
                FROM preventive p
                WHERE p.station_id = @StationId
                AND p.date_prevue_debut BETWEEN @DebutPeriode AND @FinPeriode
                ORDER BY p.id DESC;
            ";

            var tempList = new List<(Preventive preventive, int? planId, int? effId, int stationIdDb)>();

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@StationId", stationId);
                cmd.Parameters.AddWithValue("@DebutPeriode", debutPeriode);
                cmd.Parameters.AddWithValue("@FinPeriode", finPeriode);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var preventive = new Preventive
                        {
                            Id = reader.GetInt32("id"),
                            Description = reader.GetString("description"),
                            EstComplete = reader.GetBoolean("est_complete"),
                            EstAnnule = reader.GetBoolean("est_annule"),
                            DatePrevueDebut = reader.GetDateTime("date_prevue_debut"),
                            DatePrevueFin = reader.IsDBNull(reader.GetOrdinal("date_prevue_fin")) 
                                ? (DateTime?)null 
                                : reader.GetDateTime("date_prevue_fin"),
                            DateEffectiveDebut = reader.IsDBNull(reader.GetOrdinal("date_effective_debut")) 
                                ? (DateTime?)null 
                                : reader.GetDateTime("date_effective_debut"),
                            DateEffectiveFin = reader.IsDBNull(reader.GetOrdinal("date_effective_fin")) 
                                ? (DateTime?)null 
                                : reader.GetDateTime("date_effective_fin")
                        };

                        int? planId = reader.IsDBNull(reader.GetOrdinal("utilisateur_planificateur_id")) 
                            ? (int?)null 
                            : reader.GetInt32("utilisateur_planificateur_id");
                        int? effId = reader.IsDBNull(reader.GetOrdinal("utilisateur_effectif_id")) 
                            ? (int?)null 
                            : reader.GetInt32("utilisateur_effectif_id");
                        int stationIdDb = reader.GetInt32("station_id");

                        tempList.Add((preventive, planId, effId, stationIdDb));
                    }
                }
            }
            foreach (var (preventive, planId, effId, stationIdDb) in tempList)
            {
                if (planId.HasValue)
                    preventive.UtilisateurPlanificateur = UtilisateurService.GetUtilisateurById(connection, planId.Value);

                if (effId.HasValue)
                    preventive.UtilisateurEffectif = UtilisateurService.GetUtilisateurById(connection, effId.Value);

                preventive.Station = StationService.GetStationById(connection, stationIdDb);

                preventives.Add(preventive);
            }

            return preventives;
        }


        public static List<Preventive> GetPreventiveByFilter(
            MySqlConnection connection,
            string? stationNom = null,
            DateTime? datePrevueDebut = null,
            DateTime? dateEffectiveDebut = null,
            int? planificateurId = null,
            int? effectifId = null)
        {
            var preventives = new List<Preventive>();

            bool aucunFiltre = string.IsNullOrEmpty(stationNom) && !datePrevueDebut.HasValue 
                            && !dateEffectiveDebut.HasValue && !planificateurId.HasValue && !effectifId.HasValue;

            DateTime debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime finMois = debutMois.AddMonths(1).AddDays(-1);

            // Requête avec JOIN pour récupérer la station et les utilisateurs directement
            string query = @"
                SELECT p.id, p.date_prevue_debut, p.date_prevue_fin,
                    p.date_effective_debut, p.date_effective_fin,
                    p.est_complete, p.est_annule, p.description,
                    s.id as station_id, s.nom as station_nom,
                    uplan.id as planificateur_id, uplan.nom as planificateur_nom, uplan.prenom as planificateur_prenom,
                    ueff.id as effectif_id, ueff.nom as effectif_nom, ueff.prenom as effectif_prenom
                FROM preventive p
                INNER JOIN station s ON p.station_id = s.id
                INNER JOIN utilisateur uplan ON p.utilisateur_planificateur_id = uplan.id
                LEFT JOIN utilisateur ueff ON p.utilisateur_effectif_id = ueff.id
                WHERE 1=1";

            if (aucunFiltre)
                query += " AND p.date_prevue_debut BETWEEN @DebutMois AND @FinMois";
            else
            {
                if (!string.IsNullOrEmpty(stationNom))
                    query += " AND s.nom LIKE @StationNom";

                if (datePrevueDebut.HasValue)
                    query += " AND p.date_prevue_debut >= @DatePrevueDebut";

                if (dateEffectiveDebut.HasValue)
                    query += " AND p.date_effective_debut >= @DateEffectiveDebut";

                if (planificateurId.HasValue)
                    query += " AND uplan.id = @PlanId";

                if (effectifId.HasValue)
                    query += " AND ueff.id = @EffId";
            }

            query += " ORDER BY p.date_prevue_debut DESC";

            using var cmd = new MySqlCommand(query, connection);

            if (aucunFiltre)
            {
                cmd.Parameters.AddWithValue("@DebutMois", debutMois);
                cmd.Parameters.AddWithValue("@FinMois", finMois);
            }
            else
            {
                if (!string.IsNullOrEmpty(stationNom))
                    cmd.Parameters.AddWithValue("@StationNom", $"%{stationNom}%");

                if (datePrevueDebut.HasValue)
                    cmd.Parameters.AddWithValue("@DatePrevueDebut", datePrevueDebut.Value);

                if (dateEffectiveDebut.HasValue)
                    cmd.Parameters.AddWithValue("@DateEffectiveDebut", dateEffectiveDebut.Value);

                if (planificateurId.HasValue)
                    cmd.Parameters.AddWithValue("@PlanId", planificateurId.Value);

                if (effectifId.HasValue)
                    cmd.Parameters.AddWithValue("@EffId", effectifId.Value);
            }

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var preventive = new Preventive
                {
                    Id = reader.GetInt32("id"),
                    Description = reader.GetString("description"),
                    EstComplete = reader.GetBoolean("est_complete"),
                    EstAnnule = reader.GetBoolean("est_annule"),
                    DatePrevueDebut = reader.GetDateTime("date_prevue_debut"),
                    DatePrevueFin = reader.IsDBNull(reader.GetOrdinal("date_prevue_fin")) ? (DateTime?)null : reader.GetDateTime("date_prevue_fin"),
                    DateEffectiveDebut = reader.IsDBNull(reader.GetOrdinal("date_effective_debut")) ? (DateTime?)null : reader.GetDateTime("date_effective_debut"),
                    DateEffectiveFin = reader.IsDBNull(reader.GetOrdinal("date_effective_fin")) ? (DateTime?)null : reader.GetDateTime("date_effective_fin"),
                    Station = new Models.Station.Station
                    {
                        Id = reader.GetInt32("station_id"),
                        Nom = reader.GetString("station_nom")
                    },
                    UtilisateurPlanificateur = new Models.Auth.Utilisateur
                    {
                        Id = reader.GetInt32("planificateur_id"),
                        Nom = reader.GetString("planificateur_nom"),
                        Prenom = reader.GetString("planificateur_prenom")
                    },
                    UtilisateurEffectif = reader.IsDBNull(reader.GetOrdinal("effectif_id")) ? null : new Models.Auth.Utilisateur
                    {
                        Id = reader.GetInt32("effectif_id"),
                        Nom = reader.GetString("effectif_nom"),
                        Prenom = reader.GetString("effectif_prenom")
                    }
                };

                preventives.Add(preventive);
            }

            return preventives;
        }

        public static Preventive? GetPreventiveById(MySqlConnection connection, int id)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT p.id, p.date_prevue_debut, p.date_prevue_fin,
                    p.date_effective_debut, p.date_effective_fin,
                    p.est_complete, p.est_annule, p.description,
                    p.utilisateur_planificateur_id, p.utilisateur_effectif_id,
                    p.station_id
                FROM preventive p
                WHERE p.id = @Id;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            var preventive = new Preventive
            {
                Id = reader.GetInt32("id"),
                Description = reader.GetString("description"),
                EstComplete = reader.GetBoolean("est_complete"),
                EstAnnule = reader.GetBoolean("est_annule"),
                DatePrevueDebut = reader.GetDateTime("date_prevue_debut"),
                DatePrevueFin = reader.IsDBNull(reader.GetOrdinal("date_prevue_fin"))
                    ? (DateTime?)null
                    : reader.GetDateTime("date_prevue_fin"),
                DateEffectiveDebut = reader.IsDBNull(reader.GetOrdinal("date_effective_debut"))
                    ? (DateTime?)null
                    : reader.GetDateTime("date_effective_debut"),
                DateEffectiveFin = reader.IsDBNull(reader.GetOrdinal("date_effective_fin"))
                    ? (DateTime?)null
                    : reader.GetDateTime("date_effective_fin"),
            };

            int? planId = reader.IsDBNull(reader.GetOrdinal("utilisateur_planificateur_id"))
                ? (int?)null
                : reader.GetInt32("utilisateur_planificateur_id");

            int? effId = reader.IsDBNull(reader.GetOrdinal("utilisateur_effectif_id"))
                ? (int?)null
                : reader.GetInt32("utilisateur_effectif_id");

            int stationId = reader.GetInt32("station_id");

            reader.Close();

            if (planId.HasValue)
                preventive.UtilisateurPlanificateur = UtilisateurService.GetUtilisateurById(connection, planId.Value);

            if (effId.HasValue)
                preventive.UtilisateurEffectif = UtilisateurService.GetUtilisateurById(connection, effId.Value);

            preventive.Station = StationService.GetStationById(connection, stationId);

            return preventive;
        }

        public static void EffectuerPreventive(
            MySqlConnection connection,
            int preventiveId,
            int utilisateurEffectifId,
            DateTime dateEffectiveDebut,
            DateTime? dateEffectiveFin)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            bool estComplete = dateEffectiveFin.HasValue;

            string query = @"
                UPDATE preventive
                SET utilisateur_effectif_id = @EffectifId,
                    date_effective_debut = @DateDebut,
                    date_effective_fin = @DateFin,
                    est_complete = @EstComplete
                WHERE id = @Id;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@EffectifId", utilisateurEffectifId);
            cmd.Parameters.AddWithValue("@DateDebut", dateEffectiveDebut);
            cmd.Parameters.AddWithValue("@DateFin", dateEffectiveFin ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EstComplete", estComplete);
            cmd.Parameters.AddWithValue("@Id", preventiveId);

            cmd.ExecuteNonQuery();
        }

        public static void TerminerPreventive(
            MySqlConnection connection,
            int preventiveId,
            DateTime dateEffectiveFin)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                UPDATE preventive
                SET date_effective_fin = @DateFin,
                    est_complete = 1
                WHERE id = @Id;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@DateFin", dateEffectiveFin);
            cmd.Parameters.AddWithValue("@Id", preventiveId);

            cmd.ExecuteNonQuery();
        }

        public static void AnnulerPreventive(
            MySqlConnection connection,
            int preventiveId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                UPDATE preventive
                SET est_annule = 1,
                    date_effective_debut = NULL,
                    date_effective_fin = NULL
                WHERE id = @Id;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", preventiveId);

            cmd.ExecuteNonQuery();
        }

        public static bool EstPreventiveEnRetard(MySqlConnection connection, int stationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT 1
                FROM preventive
                WHERE est_complete = 0
                AND est_annule = 0
                AND date_prevue_fin IS NOT NULL
                AND date_prevue_fin < @Now
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
