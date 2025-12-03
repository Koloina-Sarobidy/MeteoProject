
using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using StationControl.Models.Station;
using StationControl.Models.Util;

namespace StationControl.Services.Station
{
    public static class PredictionService
    {
        public static PredictionBesoinMulti GetPredictionsParTypeByEquipementStation(MySqlConnection connection, int equipementStationId)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var result = new PredictionBesoinMulti
            {
                EquipementStationId = equipementStationId
            };

            string sqlAll = @"
                SELECT date, equipement_besoin_id
                FROM besoin_station
                WHERE equipement_station_id = @id
                ORDER BY date ASC;
            ";

            var toutesOccurrences = new List<(DateTime date, int besoinId)>();

            using (var cmd = new MySqlCommand(sqlAll, connection))
            {
                cmd.Parameters.AddWithValue("@id", equipementStationId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var d = reader.GetDateTime("date");
                    var bid = reader.GetInt32("equipement_besoin_id");
                    toutesOccurrences.Add((d, bid));
                }
            }

            if (toutesOccurrences.Count == 0)
            {
                return result;
            }

            result.DernierBesoinGlobal = toutesOccurrences.Last().date;

            double? frequenceGlobale = null;
            if (toutesOccurrences.Count > 1)
            {
                var intervallesGlobal = new List<double>();
                for (int i = 1; i < toutesOccurrences.Count; i++)
                {
                    var diff = (toutesOccurrences[i].date - toutesOccurrences[i - 1].date).TotalDays;
                    if (diff > 0) intervallesGlobal.Add(diff);
                }
                if (intervallesGlobal.Count > 0)
                    frequenceGlobale = intervallesGlobal.Average();
            }
            result.FrequenceMoyenneGlobalJours = frequenceGlobale;

            var groupes = toutesOccurrences
                .GroupBy(t => t.besoinId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.date).OrderBy(d => d).ToList());

            foreach (var kv in groupes)
            {
                int besoinId = kv.Key;
                var dates = kv.Value;
                var pred = new PredictionBesoinParType
                {
                    EquipementBesoinId = besoinId,
                    EquipementBesoinLibelle = null, 
                    DerniereOccurrence = dates.Last(),
                    FrequenceMoyenneJours = null,
                    DateProchaineOccurrence = null,
                    AUtiliseFallback = false
                };

                if (dates.Count > 1)
                {
                    var intervalles = new List<double>();
                    for (int i = 1; i < dates.Count; i++)
                    {
                        var diff = (dates[i] - dates[i - 1]).TotalDays;
                        if (diff > 0) intervalles.Add(diff);
                    }
                    if (intervalles.Count > 0)
                    {
                        pred.FrequenceMoyenneJours = intervalles.Average();
                        pred.DateProchaineOccurrence = pred.DerniereOccurrence?.AddDays(pred.FrequenceMoyenneJours.Value);
                    }
                }
                else
                {
                    if (frequenceGlobale.HasValue)
                    {
                        pred.FrequenceMoyenneJours = frequenceGlobale.Value;
                        pred.DateProchaineOccurrence = pred.DerniereOccurrence?.AddDays(frequenceGlobale.Value);
                        pred.AUtiliseFallback = true;
                    }
                    else
                    {
                        string qEquip = @"
                            SELECT estimation_vie_annee
                            FROM equipement_station
                            WHERE id = @id
                            LIMIT 1;
                        ";
                        using (var cmd2 = new MySqlCommand(qEquip, connection))
                        {
                            cmd2.Parameters.AddWithValue("@id", equipementStationId);
                            var res = cmd2.ExecuteScalar();
                            if (res != null && res != DBNull.Value)
                            {
                                if (double.TryParse(res.ToString(), out double estAnnee))
                                {
                                    var jours = (int)Math.Round(estAnnee * 365.0);
                                    pred.FrequenceMoyenneJours = jours;
                                    pred.DateProchaineOccurrence = pred.DerniereOccurrence?.AddDays(jours);
                                    pred.AUtiliseFallback = true;
                                }
                            }
                        }
                    }
                }

                result.ParType.Add(pred);
            }

            var besoinIds = result.ParType.Select(p => p.EquipementBesoinId).Distinct().ToList();
            if (besoinIds.Count > 0)
            {
                string inClause = string.Join(",", besoinIds);
                string qLib = $"SELECT id, libelle FROM equipement_besoin WHERE id IN ({inClause});";
                using (var cmd = new MySqlCommand(qLib, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    var map = new Dictionary<int, string>();
                    while (reader.Read())
                    {
                        map[reader.GetInt32("id")] = reader.IsDBNull(reader.GetOrdinal("libelle")) ? null : reader.GetString("libelle");
                    }

                    foreach (var p in result.ParType)
                    {
                        if (map.ContainsKey(p.EquipementBesoinId))
                            p.EquipementBesoinLibelle = map[p.EquipementBesoinId];
                    }
                }
            }
            return result;
        }
    }
}
