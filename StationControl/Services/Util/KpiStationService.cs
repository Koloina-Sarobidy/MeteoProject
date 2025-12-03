using MySql.Data.MySqlClient;

namespace StationControl.Services.Util
{
    public class KpiStationService
    {
        public static KpiStation GetKpiStation(MySqlConnection connection, int stationId)
        {
            var kpi = new KpiStation();

            // MTBF : temps moyen entre deux pannes d'un équipement
            string queryMTBF = @"
                SELECT AVG(DATEDIFF(next_failure, date)) 
                FROM (
                    SELECT bs.date AS date,
                           LEAD(bs.date) OVER(PARTITION BY es.id ORDER BY bs.date) AS next_failure
                    FROM besoin_station bs
                    INNER JOIN equipement_station es ON es.id = bs.equipement_station_id
                    WHERE es.station_id = @stationId
                ) t
                WHERE next_failure IS NOT NULL;";

            using (var cmd = new MySqlCommand(queryMTBF, connection))
            {
                cmd.Parameters.AddWithValue("@stationId", stationId);
                var result = cmd.ExecuteScalar();
                kpi.MTBF = result != DBNull.Value ? Convert.ToDouble(result) : 0;
            }

            // MTTR : temps moyen de réparation
            string queryMTTR = @"
                SELECT AVG(DATEDIFF(i.date_effective_fin, i.date_effective_debut))
                FROM intervention i
                INNER JOIN besoin_station bs ON bs.id = i.besoin_station_id
                INNER JOIN equipement_station es ON es.id = bs.equipement_station_id
                WHERE es.station_id = @stationId
                  AND i.date_effective_debut IS NOT NULL
                  AND i.date_effective_fin IS NOT NULL;";

            using (var cmd = new MySqlCommand(queryMTTR, connection))
            {
                cmd.Parameters.AddWithValue("@stationId", stationId);
                var result = cmd.ExecuteScalar();
                kpi.MTTR = result != DBNull.Value ? Convert.ToDouble(result) : 0;
            }

            // Taux de disponibilité : MTBF / (MTBF + MTTR)
            kpi.TauxDisponibilite = (kpi.MTBF + kpi.MTTR) > 0 
                ? kpi.MTBF / (kpi.MTBF + kpi.MTTR) * 100 
                : 100; 

            return kpi;
        }
    }

    public class KpiStation
    {
        public double TauxDisponibilite { get; set; } // en %
        public double MTBF { get; set; } // en jours
        public double MTTR { get; set; } // en jours
    }
}
