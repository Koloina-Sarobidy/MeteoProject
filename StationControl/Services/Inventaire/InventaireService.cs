using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using StationControl.Models.Besoin;
using StationControl.Models.Inventaire;
using StationControl.Services.Station;

namespace StationControl.Services.Inventaire
{
    public static class InventaireService
    {
        public static bool InsererInventaire(
            MySqlConnection connection,
            int stationId,
            int utilisateurId,
            List<InventaireDetail> details,
            string commentaireGlobal)
        {
            if (connection == null)
            throw new ArgumentNullException(nameof(connection));
            if (details == null || details.Count == 0)
            throw new ArgumentException("Les détails de l'inventaire sont vides.");

            using var transaction = connection.BeginTransaction();
            try
            {
                string insertInventaireQuery = @"
                    INSERT INTO inventaire (station_id, utilisateur_id, commentaire)
                    VALUES (@StationId, @UtilisateurId, @Commentaire);
                    SELECT LAST_INSERT_ID();";

                int inventaireId;
                using (var cmd = new MySqlCommand(insertInventaireQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@StationId", stationId);
                    cmd.Parameters.AddWithValue("@UtilisateurId", utilisateurId);
                    cmd.Parameters.AddWithValue("@Commentaire", commentaireGlobal);
                    inventaireId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string insertDetailQuery = @"
                    INSERT INTO inventaire_details
                    (inventaire_id, equipement_station_id, est_fonctionnel)
                    VALUES (@InventaireId, @EquipementStationId, @EstFonctionnel);";

                string insertBesoinQuery = @"
                    INSERT INTO besoin_station
                    (station_id, date, equipement_station_id, equipement_besoin_id, description_probleme)
                    VALUES (@StationId, @Date, @EquipementStationId, @EquipementBesoinId, @DescriptionProbleme);";

                string updateEquipementQuery = @"
                    UPDATE equipement_station
                    SET statut = @Statut
                    WHERE id = @EquipementStationId;";

                foreach (var detail in details)
                {
                    using (var cmdDetail = new MySqlCommand(insertDetailQuery, connection, transaction))
                    {
                        cmdDetail.Parameters.AddWithValue("@InventaireId", inventaireId);
                        cmdDetail.Parameters.AddWithValue("@EquipementStationId", detail.EquipementStationId);
                        cmdDetail.Parameters.AddWithValue("@EstFonctionnel", detail.EstFonctionnel);
                        cmdDetail.ExecuteNonQuery();
                    }

                    if (!detail.EstFonctionnel)
                    {
                        using (var cmdUpdate = new MySqlCommand(updateEquipementQuery, connection, transaction))
                        {
                            cmdUpdate.Parameters.AddWithValue("@EquipementStationId", detail.EquipementStationId);
                            cmdUpdate.Parameters.AddWithValue("@Statut", "Non Fonctionnel");
                            cmdUpdate.ExecuteNonQuery();
                        }
                    }

                    if (!detail.EstFonctionnel &&
                        detail.BesoinStation != null &&
                        detail.BesoinStation.EquipementBesoin != null)
                    {
                        using (var cmdBesoin = new MySqlCommand(insertBesoinQuery, connection, transaction))
                        {
                            cmdBesoin.Parameters.AddWithValue("@StationId", stationId);
                            cmdBesoin.Parameters.AddWithValue("@Date", DateTime.Now);
                            cmdBesoin.Parameters.AddWithValue("@EquipementStationId", detail.EquipementStationId);
                            cmdBesoin.Parameters.AddWithValue("@EquipementBesoinId", detail.BesoinStation.EquipementBesoin.Id);
                            cmdBesoin.Parameters.AddWithValue("@DescriptionProbleme",
                                string.IsNullOrEmpty(detail.BesoinStation.DescriptionProbleme) ? (object)DBNull.Value : detail.BesoinStation.DescriptionProbleme);
                            cmdBesoin.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }

        }
        public static bool EstInventaireNecessaire(MySqlConnection connection, int stationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT f.date_dernier_inventaire, f.frequence_jour
                FROM frequence_inventaire f
                WHERE f.station_id = @StationId;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@StationId", stationId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return false;
            }

            var dateDernierInventaire = reader.IsDBNull(reader.GetOrdinal("date_dernier_inventaire"))
                ? (DateTime?)null
                : reader.GetDateTime("date_dernier_inventaire");
            int frequenceJour = reader.GetInt32("frequence_jour");

            if (!dateDernierInventaire.HasValue)
                return true;

            if (dateDernierInventaire.Value.AddDays(frequenceJour) < DateTime.Now.Date)
                return true;

            return false;
        }


    }
}
