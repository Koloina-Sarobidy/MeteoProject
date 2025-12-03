using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using StationControl.Models.Crm;

namespace StationControl.Services.Crm
{
    public class CrmService
    {
        public static List<Region> GetAllRegion(MySqlConnection connection)
        {
            List<Region> regions = new List<Region>();

            string query = @"
                SELECT r.id, r.nom
                FROM region r;";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        regions.Add(new Region
                        {
                            Id = reader.GetInt32("id"),
                            Libelle = reader.GetString("nom")
                        });
                    }
                }
            }

            return regions;
        }
        public static int InsertCrm(MySqlConnection connection, Models.Crm.Crm crm)
        {
            string query = @"INSERT INTO crm (nom, date_debut, date_fin, est_arrete) 
                             VALUES (@libelle, @date_debut, @date_fin, @est_arrete);
                             SELECT LAST_INSERT_ID();";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@libelle", crm.Libelle);
                cmd.Parameters.AddWithValue("@date_debut", crm.DateDebut);
                cmd.Parameters.AddWithValue("@date_fin", (object?)crm.DateFin ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@est_arrete", (object?)crm.EstArrete ?? DBNull.Value);

                object result = cmd.ExecuteScalar();
                crm.Id = Convert.ToInt32(result);
            }

            return crm.Id;
        }
        public static void InsertCrmStation(MySqlConnection connection, int idCrm, int idStation)
        {
            string query = @"INSERT INTO crm_station (crm_id, station_id) 
                             VALUES (@crmId, @stationId);
                             SELECT LAST_INSERT_ID();";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@crmId", idCrm);
                cmd.Parameters.AddWithValue("@stationId", idStation);

                object result = cmd.ExecuteScalar();
            }
        }
        
        public static List<Models.Crm.Crm> GetAllCrms(MySqlConnection connection)
        {
            List<Models.Crm.Crm> crms = new List<Models.Crm.Crm>();

            string query = @"SELECT id, nom, date_debut, date_fin, est_arrete 
                             FROM crm where est_arrete != true
                             ORDER BY date_debut DESC;";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Models.Crm.Crm crm = new Models.Crm.Crm
                        {
                            Id = reader.GetInt32("id"),
                            Libelle = reader.GetString("nom"),
                            DateDebut = reader.GetDateTime("date_debut"),
                            DateFin = reader.IsDBNull(reader.GetOrdinal("date_fin"))
                                      ? (DateTime?)null
                                      : reader.GetDateTime("date_fin"),
                            EstArrete = reader.IsDBNull(reader.GetOrdinal("est_arrete"))
                                      ? (bool?)null
                                      : reader.GetBoolean("est_arrete"),
                        };
                        crms.Add(crm);
                    }
                }
            }
            return crms;
        }
        public static void ArreterCrm(MySqlConnection connection, int crmId, DateTime dateFin)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"UPDATE crm 
                             SET est_arrete = 1, date_fin = @dateFin 
                             WHERE id = @id";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@dateFin", dateFin);
                cmd.Parameters.AddWithValue("@id", crmId);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                    throw new Exception("CRM introuvable ou déjà arrêté.");
            }
        }
        public static Models.Crm.Crm? GetCrmById(MySqlConnection connection, int idCrm)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            Models.Crm.Crm? crm = null;

            string query = @"SELECT id, nom, date_debut, date_fin, est_arrete 
                            FROM crm 
                            WHERE id = @idCrm";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@idCrm", idCrm);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        crm = new Models.Crm.Crm
                        {
                            Id = reader.GetInt32("id"),
                            Libelle = reader.GetString("nom"),
                            DateDebut = reader.GetDateTime("date_debut"),
                            DateFin = reader.IsDBNull(reader.GetOrdinal("date_fin"))
                                    ? (DateTime?)null
                                    : reader.GetDateTime("date_fin"),
                            EstArrete = reader.IsDBNull(reader.GetOrdinal("est_arrete"))
                                    ? (bool?)null
                                    : reader.GetBoolean("est_arrete")
                        };
                    }
                }
            }
            return crm;
        }

        public static Models.Crm.Crm? GetCrmByStationId(MySqlConnection connection, int stationId)
        {
            string query = @"
                SELECT c.id, c.nom, c.date_debut, c.date_fin, c.est_arrete, c.date_maj
                FROM crm c
                INNER JOIN crm_station cs ON cs.crm_id = c.id
                WHERE cs.station_id = @stationId
                LIMIT 1;";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@stationId", stationId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var crm = new Models.Crm.Crm
                {
                    Id = reader.GetInt32("id"),
                    Libelle = reader.IsDBNull(reader.GetOrdinal("nom")) ? "" : reader.GetString("nom"),
                    DateDebut = reader.IsDBNull(reader.GetOrdinal("date_debut")) ? DateTime.MinValue : reader.GetDateTime("date_debut"),
                    DateFin = reader.IsDBNull(reader.GetOrdinal("date_fin")) ? (DateTime?)null : reader.GetDateTime("date_fin"),
                    EstArrete = reader.IsDBNull(reader.GetOrdinal("est_arrete")) ? (bool?)null : reader.GetBoolean("est_arrete"),
                    DateMaj = reader.IsDBNull(reader.GetOrdinal("date_maj")) ? DateTime.MinValue : reader.GetDateTime("date_maj")
                };

                return crm;
            }

            return null; 
        }


        public static List<Models.Crm.Crm> GetCrmsByFilter(MySqlConnection connection, string? nom = null, DateTime? dateDebut = null, bool? estArrete = null)
        {
            List<Models.Crm.Crm> crms = new List<Models.Crm.Crm>();

            string query = "SELECT id, nom, date_debut, date_fin, est_arrete FROM crm WHERE 1=1";
            if (!string.IsNullOrEmpty(nom))
                query += " AND nom LIKE @nom";
            if (dateDebut.HasValue)
                query += " AND date_debut = @dateDebut";
            if (estArrete.HasValue)
                query += " AND est_arrete = @estArrete";

            query += " ORDER BY date_debut DESC";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                if (!string.IsNullOrEmpty(nom))
                    cmd.Parameters.AddWithValue("@nom", $"%{nom}%");
                if (dateDebut.HasValue)
                    cmd.Parameters.AddWithValue("@dateDebut", dateDebut.Value);
                if (estArrete.HasValue)
                    cmd.Parameters.AddWithValue("@estArrete", estArrete.Value);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Models.Crm.Crm crm = new Models.Crm.Crm
                        {
                            Id = reader.GetInt32("id"),
                            Libelle = reader.GetString("nom"),
                            DateDebut = reader.GetDateTime("date_debut"),
                            DateFin = reader.IsDBNull(reader.GetOrdinal("date_fin")) ? (DateTime?)null : reader.GetDateTime("date_fin"),
                            EstArrete = reader.IsDBNull(reader.GetOrdinal("est_arrete")) ? (bool?)null : reader.GetBoolean("est_arrete")
                        };
                        crms.Add(crm);
                    }
                }
            }
            return crms;
        }

        public static void UpdateCrm(MySqlConnection connection, Models.Crm.Crm crm)
        {
            if (crm == null)
                throw new ArgumentNullException(nameof(crm));

            using (MySqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    string updateCrmQuery = @"UPDATE crm SET nom = @nom WHERE id = @id";
                    using (MySqlCommand cmd = new MySqlCommand(updateCrmQuery, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@nom", crm.Libelle);
                        cmd.Parameters.AddWithValue("@id", crm.Id);
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Erreur lors de la mise à jour du CRM : " + ex.Message, ex);
                }
            }
        }

    }
}
