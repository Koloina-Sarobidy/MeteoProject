using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using StationControl.Models.Besoin;
using StationControl.Models.Station;
using StationControl.Models.Equipement;
using System.Data;

namespace StationControl.Services.Besoin
{
    public static class BesoinService
    {
        public static List<BesoinStation> GetBesoinRenvoyeSuperAdmin(MySqlConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var besoins = new List<BesoinStation>();

            string query = @"
                SELECT 
                    bs.id AS bs_id, bs.date AS bs_date, bs.description_probleme, bs.est_renvoye, bs.est_traite,

                    s.id AS s_id, s.nom AS s_nom, s.latitude, s.longitude, s.statut AS s_statut,

                    es.id AS es_id, es.num_serie AS es_num_serie, es.statut AS es_statut, es.est_alimentation AS es_est_alimentation,
                    es.date_debut AS es_date_debut, es.date_fin AS es_date_fin,
                    es.capteur_id AS es_capteur_id, es.alimentation_id AS es_alimentation_id,

                    eb.id AS eb_id, eb.libelle AS eb_libelle,

                    a.id AS a_id, a.nom AS a_nom, a.description AS a_desc,
                    c.id AS c_id, c.nom AS c_nom, c.parametre AS c_param

                FROM besoin_station bs
                JOIN (
                    SELECT equipement_station_id, MAX(date) AS max_date
                    FROM besoin_station
                    WHERE est_renvoye = 1
                    GROUP BY equipement_station_id
                ) last ON last.equipement_station_id = bs.equipement_station_id
                    AND last.max_date = bs.date

                LEFT JOIN station s ON s.id = bs.station_id
                LEFT JOIN equipement_station es ON es.id = bs.equipement_station_id
                LEFT JOIN equipement_besoin eb ON eb.id = bs.equipement_besoin_id
                LEFT JOIN alimentation a ON a.id = es.alimentation_id
                LEFT JOIN capteur c ON c.id = es.capteur_id

                ORDER BY bs.date DESC;
            ";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                // Station
                Models.Station.Station station = null;
                if (!reader.IsDBNull(reader.GetOrdinal("s_id")))
                {
                    station = new Models.Station.Station
                    {
                        Id = reader.GetInt32("s_id"),
                        Nom = reader.GetString("s_nom"),
                        Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? (decimal?)null : reader.GetDecimal("latitude"),
                        Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? (decimal?)null : reader.GetDecimal("longitude"),
                        Statut = reader.IsDBNull(reader.GetOrdinal("s_statut")) ? null : reader.GetString("s_statut")
                    };
                }

                // BesoinStation
                var besoin = new BesoinStation
                {
                    Id = reader.GetInt32("bs_id"),
                    Date = reader.GetDateTime("bs_date"),
                    DescriptionProbleme = reader.IsDBNull(reader.GetOrdinal("description_probleme")) ? null : reader.GetString("description_probleme"),
                    EstRenvoye = reader.GetBoolean("est_renvoye"),
                    EstTraite = reader.GetBoolean("est_traite"),
                    Station = station,
                    EquipementStationId = reader.IsDBNull(reader.GetOrdinal("es_id")) ? 0 : reader.GetInt32("es_id"),
                    EquipementBesoin = reader.IsDBNull(reader.GetOrdinal("eb_id")) ? null : new EquipementBesoin
                    {
                        Id = reader.GetInt32("eb_id"),
                        Libelle = reader.GetString("eb_libelle")
                    }
                };

                // Alimentation ou Capteur
                if (!reader.IsDBNull(reader.GetOrdinal("es_id")))
                {
                    bool estAlimentation = !reader.IsDBNull(reader.GetOrdinal("es_est_alimentation")) 
                        && reader.GetBoolean("es_est_alimentation");

                    if (estAlimentation && !reader.IsDBNull(reader.GetOrdinal("es_alimentation_id")))
                    {
                        besoin.AlimentationStation = new AlimentationStation
                        {
                            Id = reader.GetInt32("es_id"),
                            Station = station,
                            Alimentation = new Alimentation
                            {
                                Id = reader.GetInt32("a_id"),
                                Libelle = reader.GetString("a_nom"),
                                Description = reader.IsDBNull(reader.GetOrdinal("a_desc")) ? null : reader.GetString("a_desc")
                            },
                            NumSerie = reader.GetString("es_num_serie"),
                            DateDebut = reader.GetDateTime("es_date_debut"),
                            DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                            Statut = reader.GetString("es_statut")
                        };
                    }
                    else if (!reader.IsDBNull(reader.GetOrdinal("es_capteur_id")))
                    {
                        besoin.CapteurStation = new CapteurStation
                        {
                            Id = reader.GetInt32("es_id"),
                            Station = station,
                            Capteur = new Capteur
                            {
                                Id = reader.GetInt32("c_id"),
                                Libelle = reader.GetString("c_nom"),
                                Parametre = reader.IsDBNull(reader.GetOrdinal("c_param")) ? null : reader.GetString("c_param")
                            },
                            NumSerie = reader.GetString("es_num_serie"),
                            DateDebut = reader.GetDateTime("es_date_debut"),
                            DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                            Statut = reader.GetString("es_statut")
                        };
                    }
                }

                besoins.Add(besoin);
            }

            return besoins;
        }
        public static List<BesoinStation> GetBesoinPourCrm(MySqlConnection connection, int crmId)
        {
            if (connection == null) 
                throw new ArgumentNullException(nameof(connection));

            var besoins = new List<BesoinStation>();

            var stationIds = new List<int>();
            string queryStation = "SELECT station_id FROM crm_station WHERE crm_id = @CrmId";

            using (var cmdStation = new MySqlCommand(queryStation, connection))
            {
                cmdStation.Parameters.AddWithValue("@CrmId", crmId);

                using var reader = cmdStation.ExecuteReader();
                while (reader.Read())
                {
                    stationIds.Add(reader.GetInt32("station_id"));
                }
            }

            if (!stationIds.Any())
                return besoins;

            string queryBesoin = $@"
                SELECT 
                    bs.id AS bs_id, bs.date AS bs_date, bs.description_probleme, bs.est_renvoye, bs.est_traite,
                    s.id AS s_id, s.nom AS s_nom, s.latitude, s.longitude, s.statut AS s_statut,
                    es.id AS es_id, es.num_serie AS es_num_serie, es.statut AS es_statut, es.est_alimentation AS es_est_alimentation,
                    es.date_debut AS es_date_debut, es.date_fin AS es_date_fin,
                    es.capteur_id AS es_capteur_id, es.alimentation_id AS es_alimentation_id,
                    eb.id AS eb_id, eb.libelle AS eb_libelle,
                    a.id AS a_id, a.nom AS a_nom, a.description AS a_desc,
                    c.id AS c_id, c.nom AS c_nom, c.parametre AS c_param
                FROM besoin_station bs
                LEFT JOIN station s ON s.id = bs.station_id
                LEFT JOIN equipement_station es ON es.id = bs.equipement_station_id
                LEFT JOIN equipement_besoin eb ON eb.id = bs.equipement_besoin_id
                LEFT JOIN alimentation a ON a.id = es.alimentation_id
                LEFT JOIN capteur c ON c.id = es.capteur_id
                WHERE bs.station_id IN ({string.Join(",", stationIds)})
                AND bs.est_renvoye != true
                ORDER BY bs.date DESC;
            ";

            using (var cmd = new MySqlCommand(queryBesoin, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Models.Station.Station station = null;
                    if (!reader.IsDBNull(reader.GetOrdinal("s_id")))
                    {
                        station = new Models.Station.Station
                        {
                            Id = reader.GetInt32("s_id"),
                            Nom = reader.GetString("s_nom"),
                            Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? (decimal?)null : reader.GetDecimal("latitude"),
                            Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? (decimal?)null : reader.GetDecimal("longitude"),
                            Statut = reader.IsDBNull(reader.GetOrdinal("s_statut")) ? null : reader.GetString("s_statut")
                        };
                    }

                    var besoin = new BesoinStation
                    {
                        Id = reader.GetInt32("bs_id"),
                        Date = reader.GetDateTime("bs_date"),
                        DescriptionProbleme = reader.IsDBNull(reader.GetOrdinal("description_probleme")) ? null : reader.GetString("description_probleme"),
                        EstRenvoye = reader.GetBoolean("est_renvoye"),
                        EstTraite = reader.GetBoolean("est_traite"),
                        Station = station,
                        EquipementStationId = reader.IsDBNull(reader.GetOrdinal("es_id")) ? 0 : reader.GetInt32("es_id"),
                        EquipementBesoin = reader.IsDBNull(reader.GetOrdinal("eb_id")) ? null : new EquipementBesoin
                        {
                            Id = reader.GetInt32("eb_id"),
                            Libelle = reader.GetString("eb_libelle")
                        }
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("es_id")))
                    {
                        bool estAlimentation = !reader.IsDBNull(reader.GetOrdinal("es_est_alimentation")) 
                            && reader.GetBoolean("es_est_alimentation");

                        if (estAlimentation && !reader.IsDBNull(reader.GetOrdinal("es_alimentation_id")))
                        {
                            besoin.AlimentationStation = new AlimentationStation
                            {
                                Id = reader.GetInt32("es_id"),
                                Station = station,
                                Alimentation = new Alimentation
                                {
                                    Id = reader.GetInt32("a_id"),
                                    Libelle = reader.GetString("a_nom"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("a_desc")) ? null : reader.GetString("a_desc")
                                },
                                NumSerie = reader.GetString("es_num_serie"),
                                DateDebut = reader.GetDateTime("es_date_debut"),
                                DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                                Statut = reader.GetString("es_statut")
                            };
                        }
                        else if (!reader.IsDBNull(reader.GetOrdinal("es_capteur_id")))
                        {
                            besoin.CapteurStation = new CapteurStation
                            {
                                Id = reader.GetInt32("es_id"),
                                Station = station,
                                Capteur = new Capteur
                                {
                                    Id = reader.GetInt32("c_id"),
                                    Libelle = reader.GetString("c_nom"),
                                    Parametre = reader.IsDBNull(reader.GetOrdinal("c_param")) ? null : reader.GetString("c_param")
                                },
                                NumSerie = reader.GetString("es_num_serie"),
                                DateDebut = reader.GetDateTime("es_date_debut"),
                                DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                                Statut = reader.GetString("es_statut")
                            };
                        }
                    }

                    besoins.Add(besoin);
                }
            }

            return besoins;
        }


        public static List<BesoinStation> GetBesoinPlanifies(MySqlConnection connection, int crmId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var besoins = new List<BesoinStation>();

            var regionIds = new List<int>();
            string queryRegion = @"
                SELECT DISTINCT s.region_id
                FROM crm_station cs
                JOIN station s ON cs.station_id = s.id
                WHERE cs.crm_id = @CrmId
            ";

            using (var cmdRegion = new MySqlCommand(queryRegion, connection))
            {
                cmdRegion.Parameters.AddWithValue("@CrmId", crmId);

                using var reader = cmdRegion.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("region_id")))
                        regionIds.Add(reader.GetInt32("region_id"));
                }
            }

            if (!regionIds.Any())
                return besoins;

            string queryBesoin = $@"
                SELECT 
                    bs.id AS bs_id, bs.date AS bs_date, bs.description_probleme, bs.est_renvoye, bs.est_traite,
                    s.id AS s_id, s.nom AS s_nom, s.latitude, s.longitude, s.statut AS s_statut,
                    es.id AS es_id, es.num_serie AS es_num_serie, es.statut AS es_statut, es.est_alimentation AS es_est_alimentation,
                    es.date_debut AS es_date_debut, es.date_fin AS es_date_fin,
                    es.capteur_id AS es_capteur_id, es.alimentation_id AS es_alimentation_id,
                    eb.id AS eb_id, eb.libelle AS eb_libelle,
                    a.id AS a_id, a.nom AS a_nom, a.description AS a_desc,
                    c.id AS c_id, c.nom AS c_nom, c.parametre AS c_param
                FROM besoin_station bs
                LEFT JOIN station s ON s.id = bs.station_id
                LEFT JOIN equipement_station es ON es.id = bs.equipement_station_id
                LEFT JOIN equipement_besoin eb ON eb.id = bs.equipement_besoin_id
                LEFT JOIN alimentation a ON a.id = es.alimentation_id
                LEFT JOIN capteur c ON c.id = es.capteur_id
                WHERE s.region_id IN ({string.Join(",", regionIds)})
                AND bs.est_renvoye != TRUE
                AND bs.est_traite = TRUE
                ORDER BY bs.date DESC;
            ";

            using (var cmd = new MySqlCommand(queryBesoin, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Models.Station.Station station = null;
                    if (!reader.IsDBNull(reader.GetOrdinal("s_id")))
                    {
                        station = new Models.Station.Station
                        {
                            Id = reader.GetInt32("s_id"),
                            Nom = reader.GetString("s_nom"),
                            Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? (decimal?)null : reader.GetDecimal("latitude"),
                            Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? (decimal?)null : reader.GetDecimal("longitude"),
                            Statut = reader.IsDBNull(reader.GetOrdinal("s_statut")) ? null : reader.GetString("s_statut")
                        };
                    }

                    var besoin = new BesoinStation
                    {
                        Id = reader.GetInt32("bs_id"),
                        Date = reader.GetDateTime("bs_date"),
                        DescriptionProbleme = reader.IsDBNull(reader.GetOrdinal("description_probleme")) ? null : reader.GetString("description_probleme"),
                        EstRenvoye = reader.GetBoolean("est_renvoye"),
                        EstTraite = reader.GetBoolean("est_traite"),
                        Station = station,
                        EquipementStationId = reader.IsDBNull(reader.GetOrdinal("es_id")) ? 0 : reader.GetInt32("es_id"),
                        EquipementBesoin = reader.IsDBNull(reader.GetOrdinal("eb_id")) ? null : new EquipementBesoin
                        {
                            Id = reader.GetInt32("eb_id"),
                            Libelle = reader.GetString("eb_libelle")
                        }
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("es_id")))
                    {
                        bool estAlimentation = !reader.IsDBNull(reader.GetOrdinal("es_est_alimentation")) 
                            && reader.GetBoolean("es_est_alimentation");

                        if (estAlimentation && !reader.IsDBNull(reader.GetOrdinal("es_alimentation_id")))
                        {
                            besoin.AlimentationStation = new AlimentationStation
                            {
                                Id = reader.GetInt32("es_id"),
                                Station = station,
                                Alimentation = new Alimentation
                                {
                                    Id = reader.GetInt32("a_id"),
                                    Libelle = reader.GetString("a_nom"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("a_desc")) ? null : reader.GetString("a_desc")
                                },
                                NumSerie = reader.GetString("es_num_serie"),
                                DateDebut = reader.GetDateTime("es_date_debut"),
                                DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                                Statut = reader.GetString("es_statut")
                            };
                        }
                        else if (!reader.IsDBNull(reader.GetOrdinal("es_capteur_id")))
                        {
                            besoin.CapteurStation = new CapteurStation
                            {
                                Id = reader.GetInt32("es_id"),
                                Station = station,
                                Capteur = new Capteur
                                {
                                    Id = reader.GetInt32("c_id"),
                                    Libelle = reader.GetString("c_nom"),
                                    Parametre = reader.IsDBNull(reader.GetOrdinal("c_param")) ? null : reader.GetString("c_param")
                                },
                                NumSerie = reader.GetString("es_num_serie"),
                                DateDebut = reader.GetDateTime("es_date_debut"),
                                DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                                Statut = reader.GetString("es_statut")
                            };
                        }
                    }

                    besoins.Add(besoin);
                }
            }

            return besoins;
        }



        public static Dictionary<int, EquipementBesoin> GetEquipementBesoinByIds(MySqlConnection connection, HashSet<int> ids)
        {
            var dict = new Dictionary<int, EquipementBesoin>();
            if (ids.Count == 0) return dict;

            string query = $"SELECT id, libelle FROM equipement_besoin WHERE id IN ({string.Join(",", ids)})";
            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var eb = new EquipementBesoin
                {
                    Id = reader.GetInt32("id"),
                    Libelle = reader.GetString("libelle")
                };
                dict[eb.Id] = eb;
            }
            return dict;
        }
        public static void TraiterBesoin(MySqlConnection connection, List<int> besoinIds)
        {
            if (connection == null) 
                throw new ArgumentNullException(nameof(connection));

            if (besoinIds == null || besoinIds.Count == 0)
                throw new ArgumentException("La liste de besoins est vide.");

            string query = $"UPDATE besoin_station SET est_traite = 1 WHERE id IN ({string.Join(",", besoinIds)})";

            using var cmd = new MySqlCommand(query, connection);
            cmd.ExecuteNonQuery();
        }

        public static BesoinStation GetBesoinByEquipementStation(MySqlConnection connection, int stationId, int equipementStationId)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT 
                    bs.id AS bs_id, bs.date AS bs_date, bs.description_probleme, bs.est_renvoye, bs.est_traite,

                    s.id AS s_id, s.nom AS s_nom, s.latitude, s.longitude, s.statut AS s_statut,

                    es.id AS es_id, es.num_serie AS es_num_serie, es.statut AS es_statut, es.est_alimentation AS es_est_alimentation,
                    es.date_debut AS es_date_debut, es.date_fin AS es_date_fin,
                    es.capteur_id AS es_capteur_id, es.alimentation_id AS es_alimentation_id,

                    eb.id AS eb_id, eb.libelle AS eb_libelle,

                    a.id AS a_id, a.nom AS a_nom, a.description AS a_desc,
                    c.id AS c_id, c.nom AS c_nom, c.parametre AS c_param

                FROM besoin_station bs
                LEFT JOIN station s ON s.id = bs.station_id
                LEFT JOIN equipement_station es ON es.id = bs.equipement_station_id
                LEFT JOIN equipement_besoin eb ON eb.id = bs.equipement_besoin_id
                LEFT JOIN alimentation a ON a.id = es.alimentation_id
                LEFT JOIN capteur c ON c.id = es.capteur_id

                WHERE bs.station_id = @StationId AND bs.equipement_station_id = @EquipementStationId
                ORDER BY bs.date DESC
                LIMIT 1; -- ne récupère que le dernier
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@StationId", stationId);
            cmd.Parameters.AddWithValue("@EquipementStationId", equipementStationId);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null; 

            Models.Station.Station station = null;
            if (!reader.IsDBNull(reader.GetOrdinal("s_id")))
            {
                station = new Models.Station.Station
                {
                    Id = reader.GetInt32("s_id"),
                    Nom = reader.GetString("s_nom"),
                    Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? (decimal?)null : reader.GetDecimal("latitude"),
                    Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? (decimal?)null : reader.GetDecimal("longitude"),
                    Statut = reader.IsDBNull(reader.GetOrdinal("s_statut")) ? null : reader.GetString("s_statut")
                };
            }

            var besoin = new BesoinStation
            {
                Id = reader.GetInt32("bs_id"),
                Date = reader.GetDateTime("bs_date"),
                DescriptionProbleme = reader.IsDBNull(reader.GetOrdinal("description_probleme")) ? null : reader.GetString("description_probleme"),
                EstRenvoye = reader.GetBoolean("est_renvoye"),
                EstTraite = reader.GetBoolean("est_traite"),
                Station = station,
                EquipementStationId = reader.IsDBNull(reader.GetOrdinal("es_id")) ? 0 : reader.GetInt32("es_id"),
                EquipementBesoin = reader.IsDBNull(reader.GetOrdinal("eb_id")) ? null : new EquipementBesoin
                {
                    Id = reader.GetInt32("eb_id"),
                    Libelle = reader.GetString("eb_libelle")
                }
            };

            if (!reader.IsDBNull(reader.GetOrdinal("es_id")))
            {
                bool estAlimentation = !reader.IsDBNull(reader.GetOrdinal("es_est_alimentation")) 
                    && reader.GetBoolean("es_est_alimentation");

                if (estAlimentation && !reader.IsDBNull(reader.GetOrdinal("es_alimentation_id")))
                {
                    besoin.AlimentationStation = new AlimentationStation
                    {
                        Id = reader.GetInt32("es_id"),
                        Station = station,
                        Alimentation = new Alimentation
                        {
                            Id = reader.GetInt32("a_id"),
                            Libelle = reader.GetString("a_nom"),
                            Description = reader.IsDBNull(reader.GetOrdinal("a_desc")) ? null : reader.GetString("a_desc")
                        },
                        NumSerie = reader.GetString("es_num_serie"),
                        DateDebut = reader.GetDateTime("es_date_debut"),
                        DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                        Statut = reader.GetString("es_statut")
                    };
                }
                else if (!reader.IsDBNull(reader.GetOrdinal("es_capteur_id")))
                {
                    besoin.CapteurStation = new CapteurStation
                    {
                        Id = reader.GetInt32("es_id"),
                        Station = station,
                        Capteur = new Capteur
                        {
                            Id = reader.GetInt32("c_id"),
                            Libelle = reader.GetString("c_nom"),
                            Parametre = reader.IsDBNull(reader.GetOrdinal("c_param")) ? null : reader.GetString("c_param")
                        },
                        NumSerie = reader.GetString("es_num_serie"),
                        DateDebut = reader.GetDateTime("es_date_debut"),
                        DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                        Statut = reader.GetString("es_statut")
                    };
                }
            }
            return besoin;
        }

        public static void RenvoyerSuperAdmin(MySqlConnection connection, int besoinId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (besoinId <= 0)
                throw new ArgumentException("L'identifiant du besoin est invalide.", nameof(besoinId));

            string query = "UPDATE besoin_station SET est_renvoye = 1 WHERE id = @BesoinId";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@BesoinId", besoinId);

            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected == 0)
                throw new InvalidOperationException($"Aucun besoin trouvé avec l'ID {besoinId}.");
        }

        public static List<BesoinStation> GetBesoinTraiteMaisNonCompleteByStation(MySqlConnection connection, int stationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (stationId <= 0)
                throw new ArgumentException("L'ID de la station est invalide.", nameof(stationId));

            var besoins = new List<BesoinStation>();

            string query = @"
                SELECT 
                    bs.id AS bs_id, bs.date AS bs_date, bs.description_probleme, bs.est_renvoye, bs.est_traite, bs.est_complete,
                    s.id AS s_id, s.nom AS s_nom, s.latitude, s.longitude, s.statut AS s_statut,
                    es.id AS es_id, es.num_serie AS es_num_serie, es.statut AS es_statut, es.est_alimentation AS es_est_alimentation,
                    es.date_debut AS es_date_debut, es.date_fin AS es_date_fin,
                    es.capteur_id AS es_capteur_id, es.alimentation_id AS es_alimentation_id,
                    eb.id AS eb_id, eb.libelle AS eb_libelle,
                    a.id AS a_id, a.nom AS a_nom, a.description AS a_desc,
                    c.id AS c_id, c.nom AS c_nom, c.parametre AS c_param
                FROM besoin_station bs
                LEFT JOIN station s ON s.id = bs.station_id
                LEFT JOIN equipement_station es ON es.id = bs.equipement_station_id
                LEFT JOIN equipement_besoin eb ON eb.id = bs.equipement_besoin_id
                LEFT JOIN alimentation a ON a.id = es.alimentation_id
                LEFT JOIN capteur c ON c.id = es.capteur_id
                WHERE bs.est_traite = TRUE AND bs.est_complete = FALSE AND bs.station_id = @StationId
                ORDER BY bs.date DESC;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@StationId", stationId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Models.Station.Station station = null;
                if (!reader.IsDBNull(reader.GetOrdinal("s_id")))
                {
                    station = new Models.Station.Station
                    {
                        Id = reader.GetInt32("s_id"),
                        Nom = reader.GetString("s_nom"),
                        Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? (decimal?)null : reader.GetDecimal("latitude"),
                        Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? (decimal?)null : reader.GetDecimal("longitude"),
                        Statut = reader.IsDBNull(reader.GetOrdinal("s_statut")) ? null : reader.GetString("s_statut")
                    };
                }

                var besoin = new BesoinStation
                {
                    Id = reader.GetInt32("bs_id"),
                    Date = reader.GetDateTime("bs_date"),
                    DescriptionProbleme = reader.IsDBNull(reader.GetOrdinal("description_probleme")) ? null : reader.GetString("description_probleme"),
                    EstRenvoye = reader.GetBoolean("est_renvoye"),
                    EstTraite = reader.GetBoolean("est_traite"),
                    EstComplete = reader.GetBoolean("est_complete"),
                    Station = station,
                    EquipementStationId = reader.IsDBNull(reader.GetOrdinal("es_id")) ? 0 : reader.GetInt32("es_id"),
                    EquipementBesoin = reader.IsDBNull(reader.GetOrdinal("eb_id")) ? null : new EquipementBesoin
                    {
                        Id = reader.GetInt32("eb_id"),
                        Libelle = reader.GetString("eb_libelle")
                    }
                };

                if (!reader.IsDBNull(reader.GetOrdinal("es_id")))
                {
                    bool estAlimentation = !reader.IsDBNull(reader.GetOrdinal("es_est_alimentation")) 
                        && reader.GetBoolean("es_est_alimentation");

                    if (estAlimentation && !reader.IsDBNull(reader.GetOrdinal("es_alimentation_id")))
                    {
                        besoin.AlimentationStation = new AlimentationStation
                        {
                            Id = reader.GetInt32("es_id"),
                            Station = station,
                            Alimentation = new Alimentation
                            {
                                Id = reader.GetInt32("a_id"),
                                Libelle = reader.GetString("a_nom"),
                                Description = reader.IsDBNull(reader.GetOrdinal("a_desc")) ? null : reader.GetString("a_desc")
                            },
                            NumSerie = reader.GetString("es_num_serie"),
                            DateDebut = reader.GetDateTime("es_date_debut"),
                            DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                            Statut = reader.GetString("es_statut")
                        };
                    }
                    else if (!reader.IsDBNull(reader.GetOrdinal("es_capteur_id")))
                    {
                        besoin.CapteurStation = new CapteurStation
                        {
                            Id = reader.GetInt32("es_id"),
                            Station = station,
                            Capteur = new Capteur
                            {
                                Id = reader.GetInt32("c_id"),
                                Libelle = reader.GetString("c_nom"),
                                Parametre = reader.IsDBNull(reader.GetOrdinal("c_param")) ? null : reader.GetString("c_param")
                            },
                            NumSerie = reader.GetString("es_num_serie"),
                            DateDebut = reader.GetDateTime("es_date_debut"),
                            DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                            Statut = reader.GetString("es_statut")
                        };
                    }
                }

                besoins.Add(besoin);
            }
            return besoins;
        }

        public static void CompleterBesoin(MySqlConnection connection, int besoinId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (besoinId <= 0)
                throw new ArgumentException("L'identifiant du besoin est invalide.", nameof(besoinId));

            string querySelect = @"
                SELECT bs.equipement_station_id, eb.libelle AS equipement_besoin_libelle
                FROM besoin_station bs
                JOIN equipement_besoin eb ON eb.id = bs.equipement_besoin_id
                WHERE bs.id = @BesoinId;
            ";

            int equipementStationId = 0;
            string equipementBesoinLibelle = null;

            using (var cmdSelect = new MySqlCommand(querySelect, connection))
            {
                cmdSelect.Parameters.AddWithValue("@BesoinId", besoinId);
                using var reader = cmdSelect.ExecuteReader();
                if (reader.Read())
                {
                    equipementStationId = reader.GetInt32("equipement_station_id");
                    equipementBesoinLibelle = reader.IsDBNull(reader.GetOrdinal("equipement_besoin_libelle")) 
                        ? null 
                        : reader.GetString("equipement_besoin_libelle");
                }
                else
                {
                    throw new InvalidOperationException($"Aucun besoin trouvé avec l'ID {besoinId}.");
                }
            }

            string queryUpdateBesoin = "UPDATE besoin_station SET est_complete = 1 WHERE id = @BesoinId";
            using (var cmdUpdateBesoin = new MySqlCommand(queryUpdateBesoin, connection))
            {
                cmdUpdateBesoin.Parameters.AddWithValue("@BesoinId", besoinId);
                cmdUpdateBesoin.ExecuteNonQuery();
            }

            if (equipementStationId > 0)
            {
                string queryUpdateEquipement;
                if (equipementBesoinLibelle != null && equipementBesoinLibelle.ToLower() == "besoin de remplacement")
                {
                    queryUpdateEquipement = "UPDATE equipement_station SET est_remplace = 1 WHERE id = @EquipementStationId";
                }
                else
                {
                    queryUpdateEquipement = "UPDATE equipement_station SET statut = 'Fonctionnel' WHERE id = @EquipementStationId";
                }

                using var cmdUpdateEquipement = new MySqlCommand(queryUpdateEquipement, connection);
                cmdUpdateEquipement.Parameters.AddWithValue("@EquipementStationId", equipementStationId);
                cmdUpdateEquipement.ExecuteNonQuery();
            }
        }
        public static List<BesoinStation> GetHistoriqueBesoinsAnneeEnCours(
            MySqlConnection connection,
            int stationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var besoins = new List<BesoinStation>();

            string query = @"
                SELECT 
                    bs.id AS bs_id, bs.date AS bs_date, bs.description_probleme,
                    bs.est_renvoye, bs.est_traite,

                    es.id AS es_id, es.num_serie AS es_num_serie, es.statut AS es_statut,
                    es.est_alimentation AS es_est_alimentation,
                    es.date_debut AS es_date_debut, es.date_fin AS es_date_fin,
                    es.capteur_id AS es_capteur_id, es.alimentation_id AS es_alimentation_id,

                    eb.id AS eb_id, eb.libelle AS eb_libelle,

                    a.id AS a_id, a.nom AS a_nom, a.description AS a_desc,
                    c.id AS c_id, c.nom AS c_nom, c.parametre AS c_param

                FROM besoin_station bs
                LEFT JOIN equipement_station es ON es.id = bs.equipement_station_id
                LEFT JOIN equipement_besoin eb ON eb.id = bs.equipement_besoin_id
                LEFT JOIN alimentation a ON a.id = es.alimentation_id
                LEFT JOIN capteur c ON c.id = es.capteur_id

                WHERE bs.station_id = @StationId
                AND YEAR(bs.date) = YEAR(CURDATE())
                ORDER BY bs.date ASC;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@StationId", stationId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var besoin = new BesoinStation
                {
                    Id = reader.GetInt32("bs_id"),
                    Date = reader.GetDateTime("bs_date"),
                    DescriptionProbleme = reader.IsDBNull("description_probleme") ? null : reader.GetString("description_probleme"),
                    EstRenvoye = reader.GetBoolean("est_renvoye"),
                    EstTraite = reader.GetBoolean("est_traite"),

                    EquipementStationId = reader.IsDBNull("es_id") ? 0 : reader.GetInt32("es_id"),

                    EquipementBesoin = reader.IsDBNull("eb_id") ? null :
                        new EquipementBesoin
                        {
                            Id = reader.GetInt32("eb_id"),
                            Libelle = reader.GetString("eb_libelle")
                        }
                };

                // ——————————————————————————
                //     SI L'ÉQUIPEMENT EXISTE
                // ——————————————————————————
                if (!reader.IsDBNull("es_id"))
                {
                    bool estAlim = reader.GetBoolean("es_est_alimentation");

                    if (estAlim)
                    {
                        // ALIMENTATION
                        besoin.AlimentationStation = new AlimentationStation
                        {
                            Id = reader.GetInt32("es_id"),
                            NumSerie = reader.GetString("es_num_serie"),
                            Statut = reader.GetString("es_statut"),
                            DateDebut = reader.GetDateTime("es_date_debut"),
                            DateFin = reader.IsDBNull("es_date_fin") ? null : reader.GetDateTime("es_date_fin"),

                            Alimentation = reader.IsDBNull("a_id") ? null :
                                new Alimentation
                                {
                                    Id = reader.GetInt32("a_id"),
                                    Libelle = reader.GetString("a_nom"),
                                    Description = reader.IsDBNull("a_desc") ? null : reader.GetString("a_desc")
                                }
                        };
                    }
                    else
                    {
                        // CAPTEUR
                        besoin.CapteurStation = new CapteurStation
                        {
                            Id = reader.GetInt32("es_id"),
                            NumSerie = reader.GetString("es_num_serie"),
                            Statut = reader.GetString("es_statut"),
                            DateDebut = reader.GetDateTime("es_date_debut"),
                            DateFin = reader.IsDBNull("es_date_fin") ? null : reader.GetDateTime("es_date_fin"),

                            Capteur = reader.IsDBNull("c_id") ? null :
                                new Capteur
                                {
                                    Id = reader.GetInt32("c_id"),
                                    Libelle = reader.GetString("c_nom"),
                                    Parametre = reader.IsDBNull("c_param") ? null : reader.GetString("c_param")
                                }
                        };
                    }
                }

                besoins.Add(besoin);
            }

            return besoins;
        }
        public static List<(int BesoinId, string NomStation)> GetBesoinsNonTraitesNonRenvoyesParCrm(
            MySqlConnection connection, int crmId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (crmId <= 0)
                throw new ArgumentException("L'identifiant du CRM est invalide.", nameof(crmId));

            var result = new List<(int, string)>();

            string queryBesoins = @"
                SELECT bs.id AS besoin_id, s.nom AS station_nom
                FROM besoin_station bs
                INNER JOIN station s ON s.id = bs.station_id
                INNER JOIN crm_station cs ON cs.station_id = s.id
                WHERE cs.crm_id = @CrmId
                AND bs.est_traite = FALSE
                AND bs.est_renvoye = FALSE
                ORDER BY bs.date DESC;
            ";

            using var cmd = new MySqlCommand(queryBesoins, connection);
            cmd.Parameters.AddWithValue("@CrmId", crmId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int besoinId = reader.GetInt32("besoin_id");
                string nomStation = reader.IsDBNull(reader.GetOrdinal("station_nom"))
                    ? null
                    : reader.GetString("station_nom");

                result.Add((besoinId, nomStation));
            }

            return result;
        }

        public static List<(int BesoinId, string NomStation)> GetBesoinsRenvoyesPourChefSmit(MySqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT bs.id AS besoin_id, s.nom AS station_nom
                FROM besoin_station bs
                JOIN station s ON s.id = bs.station_id
                WHERE bs.est_renvoye = TRUE and bs.est_traite != true
                ORDER BY bs.date DESC;
            ";

            var result = new List<(int, string)>();
            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int besoinId = reader.GetInt32("besoin_id");
                string nomStation = reader.IsDBNull(reader.GetOrdinal("station_nom")) 
                    ? null 
                    : reader.GetString("station_nom");

                result.Add((besoinId, nomStation));
            }

            return result;
        }
        public static BesoinStation GetBesoinById(MySqlConnection connection, int besoinId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (connection.State != ConnectionState.Open)
                throw new InvalidOperationException("MySqlConnection must be opened before calling GetBesoinById().");

            string query = @"
                SELECT 
                    bs.id AS bs_id, bs.date AS bs_date, bs.description_probleme,
                    bs.est_renvoye, bs.est_traite,

                    s.id AS s_id, s.nom AS s_nom, s.latitude, s.longitude, s.statut AS s_statut,

                    es.id AS es_id, es.num_serie AS es_num_serie, es.statut AS es_statut, 
                    es.est_alimentation AS es_est_alimentation,
                    es.date_debut AS es_date_debut, es.date_fin AS es_date_fin,
                    es.capteur_id AS es_capteur_id, es.alimentation_id AS es_alimentation_id,

                    eb.id AS eb_id, eb.libelle AS eb_libelle,

                    a.id AS a_id, a.nom AS a_nom, a.description AS a_desc,
                    c.id AS c_id, c.nom AS c_nom, c.parametre AS c_param

                FROM besoin_station bs
                LEFT JOIN station s ON s.id = bs.station_id
                LEFT JOIN equipement_station es ON es.id = bs.equipement_station_id
                LEFT JOIN equipement_besoin eb ON eb.id = bs.equipement_besoin_id
                LEFT JOIN alimentation a ON a.id = es.alimentation_id
                LEFT JOIN capteur c ON c.id = es.capteur_id

                WHERE bs.id = @BesoinId
                LIMIT 1;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@BesoinId", besoinId);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            var station = reader.IsDBNull(reader.GetOrdinal("s_id")) 
                ? null
                : new Models.Station.Station
                {
                    Id = reader.GetInt32("s_id"),
                    Nom = reader.GetString("s_nom"),
                    Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? null : reader.GetDecimal("latitude"),
                    Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? null : reader.GetDecimal("longitude"),
                    Statut = reader.IsDBNull(reader.GetOrdinal("s_statut")) ? null : reader.GetString("s_statut")
                };

            var besoin = new BesoinStation
            {
                Id = reader.GetInt32("bs_id"),
                Date = reader.GetDateTime("bs_date"),
                DescriptionProbleme = reader.IsDBNull(reader.GetOrdinal("description_probleme")) ? null : reader.GetString("description_probleme"),
                EstRenvoye = reader.GetBoolean("est_renvoye"),
                EstTraite = reader.GetBoolean("est_traite"),
                Station = station,
                EquipementStationId = reader.IsDBNull(reader.GetOrdinal("es_id")) ? 0 : reader.GetInt32("es_id"),
                EquipementBesoin = reader.IsDBNull(reader.GetOrdinal("eb_id")) ? null : new EquipementBesoin
                {
                    Id = reader.GetInt32("eb_id"),
                    Libelle = reader.GetString("eb_libelle")
                }
            };

            bool estAlim = !reader.IsDBNull(reader.GetOrdinal("es_est_alimentation"))
                        && reader.GetBoolean("es_est_alimentation");

            if (estAlim)
            {
                besoin.AlimentationStation = new AlimentationStation
                {
                    Id = reader.GetInt32("es_id"),
                    Station = station,
                    NumSerie = reader.GetString("es_num_serie"),
                    DateDebut = reader.GetDateTime("es_date_debut"),
                    DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                    Statut = reader.GetString("es_statut"),
                    Alimentation = new Alimentation
                    {
                        Id = reader.GetInt32("a_id"),
                        Libelle = reader.GetString("a_nom"),
                        Description = reader.IsDBNull(reader.GetOrdinal("a_desc")) ? null : reader.GetString("a_desc")
                    }
                };
            }
            else if (!reader.IsDBNull(reader.GetOrdinal("es_capteur_id")))
            {
                besoin.CapteurStation = new CapteurStation
                {
                    Id = reader.GetInt32("es_id"),
                    Station = station,
                    NumSerie = reader.GetString("es_num_serie"),
                    DateDebut = reader.GetDateTime("es_date_debut"),
                    DateFin = reader.IsDBNull(reader.GetOrdinal("es_date_fin")) ? null : reader.GetDateTime("es_date_fin"),
                    Statut = reader.GetString("es_statut"),
                    Capteur = new Capteur
                    {
                        Id = reader.GetInt32("c_id"),
                        Libelle = reader.GetString("c_nom"),
                        Parametre = reader.IsDBNull(reader.GetOrdinal("c_param")) ? null : reader.GetString("c_param")
                    }
                };
            }

            return besoin;
        }


    }
}
