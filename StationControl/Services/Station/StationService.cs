using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using StationControl.Models.Besoin;
using StationControl.Models.Crm;
using StationControl.Models.Equipement;
using StationControl.Models.Station;
using StationControl.Models.Util;
using StationControl.Services.Crm;

namespace StationControl.Services.Station
{
    public static class StationService
    {
        public static Region GetRegionById(MySqlConnection connection, int regionId)
        {
            string query = "SELECT id, nom FROM region WHERE id = @Id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", regionId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Region
                {
                    Id = reader.GetInt32("id"),
                    Libelle = reader.GetString("nom")
                };
            }
            return null;
        }

        public static TypeStation GetTypeStationById(MySqlConnection connection, int typeStationId)
        {
            string query = "SELECT id, libelle FROM type_station WHERE id = @Id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", typeStationId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new TypeStation
                {
                    Id = reader.GetInt32("id"),
                    Libelle = reader.GetString("libelle")
                };
            }
            return null;
        }

        public static Models.Station.Station GetStationById(MySqlConnection connection, int stationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            string query = @"
                SELECT s.id, s.nom, s.latitude, s.longitude, s.type_station_id, s.brand_id, 
                    cs.crm_id, s.region_id, s.statut, s.date_debut, s.date_fin, s.est_arrete
                FROM station s
                LEFT JOIN crm_station cs ON cs.station_id = s.id
                WHERE s.id = @Id
                ORDER BY cs.date_maj DESC
                LIMIT 1
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", stationId);

            int? typeStationId = null;
            int? brandId = null;
            int? crmId = null;
            int? regionId = null;
            int id = 0;
            string nom = null;
            decimal? latitude = null;
            decimal? longitude = null;
            string statut = null;
            DateTime dateDebut = DateTime.MinValue;
            DateTime? dateFin = null;
            bool estArrete = false;

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    id = reader.GetInt32("id");
                    nom = reader.GetString("nom");
                    latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? null : reader.GetDecimal("latitude");
                    longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? null : reader.GetDecimal("longitude");
                    typeStationId = reader.IsDBNull(reader.GetOrdinal("type_station_id")) ? null : reader.GetInt32("type_station_id");
                    brandId = reader.IsDBNull(reader.GetOrdinal("brand_id")) ? null : reader.GetInt32("brand_id");
                    crmId = reader.IsDBNull(reader.GetOrdinal("crm_id")) ? null : reader.GetInt32("crm_id");
                    regionId = reader.IsDBNull(reader.GetOrdinal("region_id")) ? null : reader.GetInt32("region_id");
                    statut = reader.IsDBNull(reader.GetOrdinal("statut")) ? null : reader.GetString("statut");
                    dateDebut = reader.GetDateTime("date_debut");
                    dateFin = reader.IsDBNull(reader.GetOrdinal("date_fin")) ? null : reader.GetDateTime("date_fin");
                    estArrete = reader.GetBoolean("est_arrete");
                }
                else
                {
                    return null;
                }
            }

            TypeStation typeStation = typeStationId.HasValue ? GetTypeStationById(connection, typeStationId.Value) : null;
            Brand brand = brandId.HasValue ? BrandService.GetBrandById(connection, brandId.Value) : null;
            Models.Crm.Crm crm = crmId.HasValue ? CrmService.GetCrmById(connection, crmId.Value) : null;
            Region region = regionId.HasValue ? GetRegionById(connection, regionId.Value) : null;

            return new Models.Station.Station
            {
                Id = id,
                Nom = nom,
                Latitude = latitude,
                Longitude = longitude,
                TypeStation = typeStation,
                Brand = brand,
                Crm = crm,
                Region = region,
                Statut = statut,
                DateDebut = dateDebut,
                DateFin = dateFin,
                EstArrete = estArrete
            };
        }


        public static List<TypeStation> GetAllTypeStation(MySqlConnection connection)
        {
            List<TypeStation> types = new List<TypeStation>();
            string query = "SELECT id, libelle FROM type_station ORDER BY libelle;";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                types.Add(new TypeStation
                {
                    Id = reader.GetInt32("id"),
                    Libelle = reader.GetString("libelle")
                });
            }
            return types;
        }
        public static int InsertStation(MySqlConnection connection, Models.Station.Station station)
        {
            string queryStation = @"
                INSERT INTO station 
                (nom, latitude, longitude, date_debut, region_id, brand_id, type_station_id)
                VALUES 
                (@libelle, @latitude, @longitude, @dateDebut, @regionId, @brandId, @typeStationId);";

            using var cmdStation = new MySqlCommand(queryStation, connection);
            cmdStation.Parameters.AddWithValue("@libelle", station.Nom);
            cmdStation.Parameters.AddWithValue("@latitude", station.Latitude);
            cmdStation.Parameters.AddWithValue("@longitude", station.Longitude);
            cmdStation.Parameters.AddWithValue("@dateDebut", station.DateDebut);
            cmdStation.Parameters.AddWithValue("@regionId", station.Region.Id);
            cmdStation.Parameters.AddWithValue("@brandId", station.Brand.Id);
            cmdStation.Parameters.AddWithValue("@typeStationId", station.TypeStation.Id);

            cmdStation.ExecuteNonQuery();

            station.Id = Convert.ToInt32(new MySqlCommand("SELECT LAST_INSERT_ID();", connection).ExecuteScalar());

            string queryInventaire = @"
                INSERT INTO frequence_inventaire 
                (station_id, frequence_jour, date_dernier_inventaire)
                VALUES
                (@stationId, @frequenceJour, @dateDernierInventaire);";

            using var cmdInventaire = new MySqlCommand(queryInventaire, connection);
            cmdInventaire.Parameters.AddWithValue("@stationId", station.Id);
            cmdInventaire.Parameters.AddWithValue("@frequenceJour", 1); 
            cmdInventaire.Parameters.AddWithValue("@dateDernierInventaire", DBNull.Value); 
            cmdInventaire.ExecuteNonQuery();

            return station.Id; 
        }



        public static List<Models.Station.Station> GetAllStation(MySqlConnection connection)
        {
            var stations = new List<Models.Station.Station>();

            string query = @"
                SELECT 
                    s.id, s.nom, s.latitude, s.longitude, s.date_debut, s.date_fin, s.est_arrete,
                    s.region_id, s.brand_id, s.type_station_id,
                    s.statut AS station_statut,
                    crm.id AS crm_id, crm.nom AS crm_nom
                FROM station s
                LEFT JOIN crm_station cs ON cs.station_id = s.id
                LEFT JOIN crm ON crm.id = cs.crm_id
                WHERE s.est_arrete != TRUE
                ORDER BY s.nom;
            ";

            var tempStations = new List<(Models.Station.Station station, int? regionId, int? brandId, int? typeId, int? crmId, string crmNom)>();

            using (var cmd = new MySqlCommand(query, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var station = new Models.Station.Station
                    {
                        Id = reader.GetInt32("id"),
                        Nom = reader.IsDBNull(reader.GetOrdinal("nom")) ? "" : reader.GetString("nom"),
                        Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? 0 : reader.GetDecimal("latitude"),
                        Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? 0 : reader.GetDecimal("longitude"),
                        DateDebut = reader.IsDBNull(reader.GetOrdinal("date_debut")) ? DateTime.MinValue : reader.GetDateTime("date_debut"),
                        DateFin = reader.IsDBNull(reader.GetOrdinal("date_fin")) ? (DateTime?)null : reader.GetDateTime("date_fin"),
                        EstArrete = reader.IsDBNull(reader.GetOrdinal("est_arrete")) ? (bool?)null : reader.GetBoolean("est_arrete"),
                        Statut = reader.IsDBNull(reader.GetOrdinal("station_statut")) ? "Non Définie" : reader.GetString("station_statut")
                    };

                    int? regionId = reader.IsDBNull(reader.GetOrdinal("region_id")) ? null : reader.GetInt32("region_id");
                    int? brandId = reader.IsDBNull(reader.GetOrdinal("brand_id")) ? null : reader.GetInt32("brand_id");
                    int? typeId = reader.IsDBNull(reader.GetOrdinal("type_station_id")) ? null : reader.GetInt32("type_station_id");
                    int? crmId = reader.IsDBNull(reader.GetOrdinal("crm_id")) ? null : reader.GetInt32("crm_id");
                    string crmNom = reader.IsDBNull(reader.GetOrdinal("crm_nom")) ? null : reader.GetString("crm_nom");

                    tempStations.Add((station, regionId, brandId, typeId, crmId, crmNom));
                }
            }

            foreach (var (station, regionId, brandId, typeId, crmId, crmNom) in tempStations)
            {
                if (regionId.HasValue)
                    station.Region = GetRegionById(connection, regionId.Value);

                if (brandId.HasValue)
                    station.Brand = BrandService.GetBrandById(connection, brandId.Value);

                if (typeId.HasValue)
                    station.TypeStation = GetTypeStationById(connection, typeId.Value);

                if (crmId.HasValue)
                    station.Crm = new Models.Crm.Crm
                    {
                        Id = crmId.Value,
                        Libelle = crmNom
                    };

                stations.Add(station);
            }

            return stations;
        }


        public static void ArretStation(MySqlConnection connection, int stationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                UPDATE station
                SET date_fin = @dateFin,
                    est_arrete = 1
                WHERE id = @stationId";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@dateFin", DateTime.Now);
            cmd.Parameters.AddWithValue("@stationId", stationId);

            cmd.ExecuteNonQuery();
        }
        public static List<CapteurStation> GetCapteurByStation(MySqlConnection connection, int stationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var list = new List<CapteurStation>();

            string query = @"
                SELECT 
                    es.id,
                    es.num_serie,
                    es.statut,
                    es.capteur_id,
                    es.est_alimentation,
                    es.est_remplace
                FROM equipement_station es
                WHERE es.station_id = @stationId and es.est_remplace != true 
                  AND (es.est_alimentation = FALSE OR es.est_alimentation IS NULL);";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@stationId", stationId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var capteurStation = new CapteurStation
                {
                    Id = reader.GetInt32("id"),
                    NumSerie = reader.IsDBNull(reader.GetOrdinal("num_serie")) ? "" : reader.GetString("num_serie"),
                    Statut = reader.IsDBNull(reader.GetOrdinal("statut")) ? "Inconnu" : reader.GetString("statut"),
                    Capteur = new Capteur
                    {
                        Id = reader.IsDBNull(reader.GetOrdinal("capteur_id")) ? 0 : reader.GetInt32("capteur_id")
                    },
                    Station = new Models.Station.Station
                    {
                        Id = stationId
                    },
                    EstRemplace = reader.IsDBNull(reader.GetOrdinal("est_remplace")) 
                        ? (bool?)null 
                        : reader.GetBoolean("est_remplace")
                };

                list.Add(capteurStation);
            }

            reader.Close();

            foreach (var cs in list)
            {
                if (cs.Capteur != null && cs.Capteur.Id > 0)
                {
                    cs.Capteur = CapteurService.GetCapteurById(connection, cs.Capteur.Id);
                }
            }

            return list;
        }
        public static List<AlimentationStation> GetAlimentationByStation(MySqlConnection connection, int stationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var list = new List<AlimentationStation>();

            string query = @"
                SELECT 
                    es.id,
                    es.num_serie,
                    es.statut,
                    es.alimentation_id,
                    es.est_remplace 
                FROM equipement_station es
                WHERE es.station_id = @stationId and es.est_remplace != true
                  AND es.est_alimentation = TRUE;";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@stationId", stationId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var alimentationStation = new AlimentationStation
                {
                    Id = reader.GetInt32("id"),
                    NumSerie = reader.IsDBNull(reader.GetOrdinal("num_serie")) ? "" : reader.GetString("num_serie"),
                    Statut = reader.IsDBNull(reader.GetOrdinal("statut")) ? "Inconnu" : reader.GetString("statut"),
                    Alimentation = new Alimentation
                    {
                        Id = reader.IsDBNull(reader.GetOrdinal("alimentation_id")) ? 0 : reader.GetInt32("alimentation_id")
                    },
                    Station = new Models.Station.Station
                    {
                        Id = stationId
                    },
                    EstRemplace = reader.IsDBNull(reader.GetOrdinal("est_remplace")) 
                        ? (bool?)null 
                        : reader.GetBoolean("est_remplace")
                };

                list.Add(alimentationStation);
            }

            reader.Close();

            foreach (var a in list)
            {
                if (a.Alimentation != null && a.Alimentation.Id > 0)
                {
                    a.Alimentation = AlimentationService.GetAlimentationById(connection, a.Alimentation.Id);
                }
            }

            return list;
        }

        public static void InsertCapteurStation(MySqlConnection connection, CapteurStation capteur)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (capteur == null) throw new ArgumentNullException(nameof(capteur));

            string query = @"
                INSERT INTO equipement_station 
                (station_id, capteur_id, num_serie, statut, date_debut, date_fin, estimation_vie_annee)
                VALUES (@StationId, @CapteurId, @NumSerie, @Statut, @DateDebut, @DateFin, @EstimationVieAnnee);";

            using var cmd = new MySqlCommand(query, connection);

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                    connection.Open();

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@StationId", capteur.Station?.Id ?? 0);
                cmd.Parameters.AddWithValue("@CapteurId", capteur.Capteur?.Id ?? 0);
                cmd.Parameters.AddWithValue("@NumSerie", capteur.NumSerie ?? "");
                cmd.Parameters.AddWithValue("@Statut", capteur.Statut ?? "Fonctionnel");
                cmd.Parameters.AddWithValue("@DateDebut", capteur.DateDebut);
                cmd.Parameters.AddWithValue("@DateFin", capteur.DateFin.HasValue ? capteur.DateFin.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EstimationVieAnnee", capteur.EstimationVieAnnee.HasValue ? capteur.EstimationVieAnnee.Value : (object)DBNull.Value);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de l'insertion du capteur dans la station : " + ex.Message, ex);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        public static void InsertAlimentationStation(MySqlConnection connection, AlimentationStation alim)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (alim == null) throw new ArgumentNullException(nameof(alim));

            string query = @"
                INSERT INTO equipement_station
                (station_id, alimentation_id, capteur_id, num_serie, date_debut, date_fin, estimation_vie_annee, est_alimentation, statut)
                VALUES
                (@StationId, @AlimentationId, @CapteurId, @NumSerie, @DateDebut, @DateFin, @EstimationVieAnnee, @EstAlimentation, @Statut);";

            using var cmd = new MySqlCommand(query, connection);

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                    connection.Open();

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@StationId", alim.Station?.Id ?? 0);
                cmd.Parameters.AddWithValue("@AlimentationId", alim.Alimentation?.Id ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CapteurId", DBNull.Value); 
                cmd.Parameters.AddWithValue("@NumSerie", alim.NumSerie ?? "");
                cmd.Parameters.AddWithValue("@DateDebut", alim.DateDebut);
                cmd.Parameters.AddWithValue("@DateFin", alim.DateFin.HasValue ? alim.DateFin.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EstimationVieAnnee", alim.EstimationVieAnnee.HasValue ? alim.EstimationVieAnnee.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EstAlimentation", true);
                cmd.Parameters.AddWithValue("@Statut", alim.Statut ?? "Fonctionnel");

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de l'insertion de l'alimentation dans la station : " + ex.Message, ex);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }
        public static List<Models.Station.Station> GetAllStationByCrm(MySqlConnection connection, int crmId)
        {
            List<Models.Station.Station> stations = new List<Models.Station.Station>();

            string query = @"
                SELECT s.id, s.nom, s.latitude, s.longitude, s.type_station_id, s.region_id, s.statut, s.brand_id, s.date_debut, s.date_fin, s.est_arrete, cr.crm_id
                FROM station s
                INNER JOIN crm_station cr ON s.id = cr.station_id
                WHERE cr.crm_id = @crm_id and s.est_arrete != true;";

            var tempList = new List<(Models.Station.Station station, int typeId, int regionId, int brandId)>();

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@crm_id", crmId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var station = new Models.Station.Station
                        {
                            Id = reader.GetInt32("id"),
                            Nom = reader.GetString("nom"),
                            Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? null : reader.GetDecimal("latitude"),
                            Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? null : reader.GetDecimal("longitude"),
                            Statut = reader.IsDBNull(reader.GetOrdinal("statut")) ? null : reader.GetString("statut"),
                            DateDebut = reader.GetDateTime("date_debut"),
                            DateFin = reader.IsDBNull(reader.GetOrdinal("date_fin")) ? null : reader.GetDateTime("date_fin"),
                            EstArrete = reader.IsDBNull(reader.GetOrdinal("est_arrete")) ? (bool?)null : reader.GetBoolean("est_arrete"),
                        };

                        int typeStationId = reader.IsDBNull(reader.GetOrdinal("type_station_id")) ? 0 : reader.GetInt32("type_station_id");
                        int regionId = reader.IsDBNull(reader.GetOrdinal("region_id")) ? 0 : reader.GetInt32("region_id");
                        int brandId = reader.IsDBNull(reader.GetOrdinal("brand_id")) ? 0 : reader.GetInt32("brand_id");
                        int crm = reader.IsDBNull(reader.GetOrdinal("crm_id")) ? 0 : reader.GetInt32("crm_id");

                        tempList.Add((station, typeStationId, regionId, brandId));
                    }
                }
            }

            foreach (var (station, typeId, regionId, brandId) in tempList)
            {
                station.TypeStation = typeId != 0 ? GetTypeStationById(connection, typeId) : null;
                station.Region = regionId != 0 ? GetRegionById(connection, regionId) : null;
                station.Crm = CrmService.GetCrmById(connection, crmId);
                stations.Add(station);
            }

            return stations;
        }
        public static EquipementStation GetEquipementStationById(MySqlConnection connection, int equipementStationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT es.id, es.station_id, es.capteur_id, es.alimentation_id, es.importance_id,
                    es.num_serie, es.date_debut, es.date_fin, es.est_alimentation, es.statut,
                FROM equipement_station es";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", equipementStationId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var equipement = new EquipementStation
                {
                    Id = reader.GetInt32("id"),
                    NumSerie = reader.IsDBNull(reader.GetOrdinal("num_serie")) ? "" : reader.GetString("num_serie"),
                    Statut = reader.IsDBNull(reader.GetOrdinal("statut")) ? "Inconnu" : reader.GetString("statut"),
                    Station = new Models.Station.Station { Id = reader.GetInt32("station_id") },
                    CapteurId = reader.IsDBNull(reader.GetOrdinal("capteur_id")) ? (int?)null : reader.GetInt32("capteur_id"),
                    AlimentationId = reader.IsDBNull(reader.GetOrdinal("alimentation_id")) ? (int?)null : reader.GetInt32("alimentation_id"),
                    EstAlimentation = reader.IsDBNull(reader.GetOrdinal("est_alimentation")) ? false : reader.GetBoolean("est_alimentation"),
                    DateDebut = reader.IsDBNull(reader.GetOrdinal("date_debut")) ? DateTime.MinValue : reader.GetDateTime("date_debut"),
                    DateFin = reader.IsDBNull(reader.GetOrdinal("date_fin")) ? (DateTime?)null : reader.GetDateTime("date_fin")
                };

                return equipement;
            }

            return null;
        }
        
        public static List<Models.Station.Station> GetStationFiltered(
            MySqlConnection connection,
            string nom = null,
            int? brandId = null,
            int? regionId = null,
            int? crmId = null,
            bool? estArrete = null,
            DateTime? dateDebut = null,
            DateTime? dateFin = null)
        {
            var stations = new List<Models.Station.Station>();
            var tempList = new List<(Models.Station.Station station, int? typeId, int? regionId, int? brandId, int? crmId)>();

            var query = @"
                SELECT s.id, s.nom, s.latitude, s.longitude, s.type_station_id, s.region_id, s.brand_id, s.est_arrete, s.date_debut, s.date_fin,
                    cs.crm_id
                FROM station s
                LEFT JOIN crm_station cs ON cs.station_id = s.id
                WHERE 1=1
            ";

            if (!string.IsNullOrEmpty(nom)) query += " AND s.nom LIKE @Nom";
            if (brandId.HasValue) query += " AND s.brand_id = @BrandId";
            if (regionId.HasValue) query += " AND s.region_id = @RegionId";
            if (crmId.HasValue) query += " AND cs.crm_id = @CrmId";
            if (estArrete.HasValue) query += " AND s.est_arrete = @EstArrete";
            if (dateDebut.HasValue) query += " AND s.date_debut >= @DateDebut";
            if (dateFin.HasValue) query += " AND (s.date_fin <= @DateFin OR s.date_fin IS NULL)";

            query += " GROUP BY s.id ORDER BY s.nom;"; 

            using var cmd = new MySqlCommand(query, connection);

            if (!string.IsNullOrEmpty(nom)) cmd.Parameters.AddWithValue("@Nom", $"%{nom}%");
            if (brandId.HasValue) cmd.Parameters.AddWithValue("@BrandId", brandId.Value);
            if (regionId.HasValue) cmd.Parameters.AddWithValue("@RegionId", regionId.Value);
            if (crmId.HasValue) cmd.Parameters.AddWithValue("@CrmId", crmId.Value);
            if (estArrete.HasValue) cmd.Parameters.AddWithValue("@EstArrete", estArrete.Value);
            if (dateDebut.HasValue) cmd.Parameters.AddWithValue("@DateDebut", dateDebut.Value);
            if (dateFin.HasValue) cmd.Parameters.AddWithValue("@DateFin", dateFin.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var station = new Models.Station.Station
                {
                    Id = reader.GetInt32("id"),
                    Nom = reader.IsDBNull(reader.GetOrdinal("nom")) ? "" : reader.GetString("nom"),
                    Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? null : reader.GetDecimal("latitude"),
                    Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? null : reader.GetDecimal("longitude"),
                    DateDebut = reader.GetDateTime("date_debut"),
                    DateFin = reader.IsDBNull(reader.GetOrdinal("date_fin")) ? null : reader.GetDateTime("date_fin"),
                    EstArrete = reader.IsDBNull(reader.GetOrdinal("est_arrete")) ? (bool?)null : reader.GetBoolean("est_arrete"),
                };

                int? typeStationId = reader.IsDBNull(reader.GetOrdinal("type_station_id")) ? null : reader.GetInt32("type_station_id");
                int? regionIdVal = reader.IsDBNull(reader.GetOrdinal("region_id")) ? null : reader.GetInt32("region_id");
                int? brandIdVal = reader.IsDBNull(reader.GetOrdinal("brand_id")) ? null : reader.GetInt32("brand_id");
                int? crmIdVal = reader.IsDBNull(reader.GetOrdinal("crm_id")) ? null : reader.GetInt32("crm_id");

                tempList.Add((station, typeStationId, regionIdVal, brandIdVal, crmIdVal));
            }

            reader.Close();

            foreach (var (station, typeId, regId, brId, crmIdVal) in tempList)
            {
                station.TypeStation = typeId.HasValue ? GetTypeStationById(connection, typeId.Value) : null;
                station.Region = regId.HasValue ? GetRegionById(connection, regId.Value) : null;
                station.Brand = brId.HasValue ? BrandService.GetBrandById(connection, brId.Value) : null;
                station.Crm = crmIdVal.HasValue ? CrmService.GetCrmById(connection, crmIdVal.Value) : null;

                stations.Add(station);
            }

            return stations;
        }



        public static List<EquipementBesoin> GetAllEquipementBesoin(MySqlConnection connection)
        {
            List<EquipementBesoin> besoins = new List<EquipementBesoin>();

            string query = "SELECT id, libelle FROM equipement_besoin ORDER BY libelle;";

            using (var cmd = new MySqlCommand(query, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    besoins.Add(new EquipementBesoin
                    {
                        Id = reader.GetInt32("id"),
                        Libelle = reader.GetString("libelle")
                    });
                }
            }

            return besoins;
        }
        public static EquipementBesoin GetEquipementBesoinById(MySqlConnection connection, int equipementBesoinId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT id, libelle
                FROM equipement_besoin
                WHERE id = @id;";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", equipementBesoinId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new EquipementBesoin
                {
                    Id = reader.GetInt32("id"),
                    Libelle = reader.IsDBNull(reader.GetOrdinal("libelle")) ? null : reader.GetString("libelle"),
                };
            }

            return null;
        }

        public static Dictionary<int, Dictionary<string, double>> GetStationStatusPercentageByCrm(MySqlConnection connection, List<int> crmIds)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var result = new Dictionary<int, Dictionary<string, double>>();

            foreach (var crmId in crmIds)
            {
                var statusCount = new Dictionary<string, int>
                {
                    { "Fonctionnelle", 0 },
                    { "Partiellement Fonctionnelle", 0 },
                    { "Non Fonctionnelle", 0 }
                };

                string query = @"
                    SELECT s.statut, COUNT(*) as count
                    FROM station s
                    INNER JOIN crm_station cs ON cs.station_id = s.id
                    WHERE cs.crm_id = @crm_id AND s.est_arrete != TRUE
                    GROUP BY s.statut;
                ";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@crm_id", crmId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var statut = reader.IsDBNull(reader.GetOrdinal("statut"))
                        ? "Non Définie"
                        : reader.GetString("statut");

                    var count = reader.GetInt32("count");

                    if (!statusCount.ContainsKey(statut))
                        statusCount[statut] = 0;

                    statusCount[statut] = count;
                }
                reader.Close();

                int total = statusCount.Values.Sum();

                var statusPercentage = statusCount.ToDictionary(
                    k => k.Key,
                    k => total > 0 ? (k.Value * 100.0 / total) : 0.0
                );

                result[crmId] = statusPercentage;
            }

            return result;
        }

        public static Dictionary<string, int> GetCurativeInterventionCountByStationCurrentMonth(MySqlConnection connection)
        {
            var results = new Dictionary<string, int>();

            string query = @"
                SELECT s.nom AS station_name, COUNT(i.id) AS total
                FROM intervention i
                INNER JOIN station s ON s.id = i.station_id
                WHERE i.statut = 'Terminée'
                AND MONTH(i.date_intervention) = MONTH(CURRENT_DATE())
                AND YEAR(i.date_intervention) = YEAR(CURRENT_DATE())
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

        public static List<StatutBrandStats> GetStatutBrandStats(MySqlConnection connection)
        {
            var results = new List<StatutBrandStats>();

            string query = @"
                SELECT
                    b.nom AS brand_name,
                    COUNT(s.id) AS total_stations,

                    SUM(CASE WHEN s.statut = 'Fonctionnelle' THEN 1 ELSE 0 END) AS fonctionnelle,
                    SUM(CASE WHEN s.statut = 'Partiellement Fonctionnelle' THEN 1 ELSE 0 END) AS partielle,
                    SUM(CASE WHEN s.statut = 'Non Fonctionnelle' THEN 1 ELSE 0 END) AS non_fonctionnelle,

                    ROUND((SUM(CASE WHEN s.statut = 'Fonctionnelle' THEN 1 ELSE 0 END) / COUNT(s.id)) * 100, 2) AS taux_fonctionnelle,
                    ROUND((SUM(CASE WHEN s.statut = 'Partiellement Fonctionnelle' THEN 1 ELSE 0 END) / COUNT(s.id)) * 100, 2) AS taux_partielle,
                    ROUND((SUM(CASE WHEN s.statut = 'Non Fonctionnelle' THEN 1 ELSE 0 END) / COUNT(s.id)) * 100, 2) AS taux_non_fonctionnelle

                FROM station s
                INNER JOIN brand b ON b.id = s.brand_id
                WHERE s.est_arrete != TRUE
                GROUP BY b.nom
                ORDER BY b.nom;
            ";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new StatutBrandStats
                {
                    BrandName = reader.GetString("brand_name"),
                    TotalStations = reader.GetInt32("total_stations"),

                    Fonctionnelle = reader.GetInt32("fonctionnelle"),
                    Partielle = reader.GetInt32("partielle"),
                    NonFonctionnelle = reader.GetInt32("non_fonctionnelle"),

                    TauxFonctionnelle = reader.GetDouble("taux_fonctionnelle"),
                    TauxPartielle = reader.GetDouble("taux_partielle"),
                    TauxNonFonctionnelle = reader.GetDouble("taux_non_fonctionnelle")
                });
            }

            return results;
        }


        public static List<StationComplet> GetAllStationComplet(MySqlConnection connection)
        {
            var stations = new List<StationComplet>();

            string query = "SELECT * FROM vue_stations_complet;";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                stations.Add(new StationComplet
                {
                    Crm = reader.IsDBNull(reader.GetOrdinal("crm")) ? null : reader.GetString("crm"),
                    Region = reader.IsDBNull(reader.GetOrdinal("region")) ? null : reader.GetString("region"),
                    CityName = reader.IsDBNull(reader.GetOrdinal("city_name")) ? null : reader.GetString("city_name"),
                    Type = reader.IsDBNull(reader.GetOrdinal("type")) ? null : reader.GetString("type"),
                    Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? null : reader.GetDecimal("longitude"),
                    Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? null : reader.GetDecimal("latitude"),
                    Etat = reader.IsDBNull(reader.GetOrdinal("etat")) ? null : reader.GetString("etat"),
                    ManufacturerBrand = reader.IsDBNull(reader.GetOrdinal("manufacturer_brand")) ? null : reader.GetString("manufacturer_brand"),
                    ActionAEntreprendre = reader.IsDBNull(reader.GetOrdinal("action_a_entreprendre")) ? null : reader.GetString("action_a_entreprendre"),
                    EquipementStationId = reader.IsDBNull(reader.GetOrdinal("equipement_station_id")) ? 0 : reader.GetInt32("equipement_station_id"),
                    EquipementLibelle = reader.IsDBNull(reader.GetOrdinal("equipement_libelle")) ? null : reader.GetString("equipement_libelle"),
                    EquipementStatut = reader.IsDBNull(reader.GetOrdinal("equipement_statut")) ? null : reader.GetString("equipement_statut"),
                    EstAlimentation = reader.IsDBNull(reader.GetOrdinal("est_alimentation")) ? false : reader.GetBoolean("est_alimentation")
                });
            }

            return stations;
        }
        public static List<Models.Station.Station> GetStationByStatut(MySqlConnection connection, string statut)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var stations = new List<Models.Station.Station>();
            var tempList = new List<(Models.Station.Station station, int? typeId, int? regionId, int? brandId)>();

            string query = @"
                SELECT s.id, s.nom, s.latitude, s.longitude, s.type_station_id, s.region_id, s.brand_id, s.est_arrete, s.date_debut, s.date_fin, s.statut
                FROM station s
                WHERE s.statut = @statut AND s.est_arrete != TRUE
                ORDER BY s.nom;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@statut", statut);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var station = new Models.Station.Station
                {
                    Id = reader.GetInt32("id"),
                    Nom = reader.IsDBNull(reader.GetOrdinal("nom")) ? "" : reader.GetString("nom"),
                    Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? null : reader.GetDecimal("latitude"),
                    Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? null : reader.GetDecimal("longitude"),
                    DateDebut = reader.GetDateTime("date_debut"),
                    DateFin = reader.IsDBNull(reader.GetOrdinal("date_fin")) ? null : reader.GetDateTime("date_fin"),
                    EstArrete = reader.IsDBNull(reader.GetOrdinal("est_arrete")) ? (bool?)null : reader.GetBoolean("est_arrete"),
                    Statut = reader.IsDBNull(reader.GetOrdinal("statut")) ? "" : reader.GetString("statut")
                };

                int? typeStationId = reader.IsDBNull(reader.GetOrdinal("type_station_id")) ? null : reader.GetInt32("type_station_id");
                int? regionId = reader.IsDBNull(reader.GetOrdinal("region_id")) ? null : reader.GetInt32("region_id");
                int? brandId = reader.IsDBNull(reader.GetOrdinal("brand_id")) ? null : reader.GetInt32("brand_id");

                tempList.Add((station, typeStationId, regionId, brandId));
            }

            reader.Close();

            foreach (var (station, typeId, regId, brId) in tempList)
            {
                station.TypeStation = typeId.HasValue ? GetTypeStationById(connection, typeId.Value) : null;
                station.Region = regId.HasValue ? GetRegionById(connection, regId.Value) : null;
                station.Brand = brId.HasValue ? BrandService.GetBrandById(connection, brId.Value) : null;

                stations.Add(station);
            }

            return stations;
        }


    }
}


