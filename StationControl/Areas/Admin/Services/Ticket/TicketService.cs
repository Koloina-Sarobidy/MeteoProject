using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using StationControl.Models.Ticket;
using StationControl.Services.Crm;
using StationControl.Services.Station;

namespace StationControl.Areas.Admin.Services.Ticket
{
    public static class TicketService
    {
        public static List<TicketListItem> GetTicketsNonVusPourCrm(MySqlConnection connection, int crmId, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT t.id, t.objet, t.description, t.date_creation,
                       t.crm_id, t.station_id
                FROM ticket t
                INNER JOIN ticket_visibilite tvs ON t.id = tvs.ticket_id
                LEFT JOIN ticket_vue tv ON t.id = tv.ticket_id AND tv.crm_id = @CrmId
                WHERE tvs.crm_id = @CrmId
                  AND tv.id IS NULL";

            if (dateDebut.HasValue)
                query += " AND DATE(t.date_creation) >= DATE(@DateDebut)";
            if (dateFin.HasValue)
                query += " AND DATE(t.date_creation) <= DATE(@DateFin)";

            query += " ORDER BY t.date_creation DESC;";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@CrmId", crmId);
            if (dateDebut.HasValue) cmd.Parameters.AddWithValue("@DateDebut", dateDebut.Value);
            if (dateFin.HasValue) cmd.Parameters.AddWithValue("@DateFin", dateFin.Value);

            var tickets = new List<TicketListItem>();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tickets.Add(new TicketListItem
                    {
                        Id = reader.GetInt32("id"),
                        Objet = reader.GetString("objet"),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                        DateCreation = reader.GetDateTime("date_creation"),
                        DejaVu = false,
                        Crm = reader.IsDBNull(reader.GetOrdinal("crm_id")) ? null : new Models.Crm.Crm { Id = reader.GetInt32("crm_id") },
                        Station = reader.IsDBNull(reader.GetOrdinal("station_id")) ? null : new Models.Station.Station { Id = reader.GetInt32("station_id") },
                        CrmDestinataire = new List<Models.Crm.Crm>(),
                        StationDestinataire = new List<Models.Station.Station>(),
                        SuperAdminDestinataire = null
                    });
                }
            }

            LoadDestinatairesGlobal(connection, tickets);
            return tickets;
        }
        public static List<TicketListItem> GetTicketsRecusPourCrm(
            MySqlConnection connection, int crmId, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT t.id, t.objet, t.description, t.date_creation,
                       t.crm_id, t.station_id, t.super_admin,
                       CASE WHEN tv.id IS NOT NULL THEN 1 ELSE 0 END AS deja_vu
                FROM ticket t
                INNER JOIN ticket_visibilite tvs ON t.id = tvs.ticket_id
                LEFT JOIN ticket_vue tv ON t.id = tv.ticket_id AND tv.crm_id = @CrmId
                WHERE tvs.crm_id = @CrmId";

            if (dateDebut.HasValue) query += " AND t.date_creation >= @DateDebut";
            if (dateFin.HasValue) query += " AND t.date_creation <= @DateFin";

            query += " ORDER BY t.date_creation DESC;";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@CrmId", crmId);
            if (dateDebut.HasValue) cmd.Parameters.AddWithValue("@DateDebut", dateDebut.Value.Date);
            if (dateFin.HasValue) cmd.Parameters.AddWithValue("@DateFin", dateFin.Value.Date);

            var tickets = new List<TicketListItem>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tickets.Add(new TicketListItem
                    {
                        Id = reader.GetInt32("id"),
                        Objet = reader.GetString("objet"),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                        DateCreation = reader.GetDateTime("date_creation"),
                        DejaVu = reader.GetInt32("deja_vu") == 1,
                        Crm = reader.IsDBNull(reader.GetOrdinal("crm_id")) ? null : new Models.Crm.Crm { Id = reader.GetInt32("crm_id") },
                        Station = reader.IsDBNull(reader.GetOrdinal("station_id")) ? null : new Models.Station.Station { Id = reader.GetInt32("station_id") },
                        SuperAdmin = !reader.IsDBNull(reader.GetOrdinal("super_admin")) && reader.GetBoolean(reader.GetOrdinal("super_admin")),
                        CrmDestinataire = new List<Models.Crm.Crm>(),
                        StationDestinataire = new List<Models.Station.Station>(),
                        SuperAdminDestinataire = null
                    });
                }
            }

            LoadDestinatairesGlobal(connection, tickets);

            return tickets;
        }

        public static List<TicketListItem> GetTicketsEnvoyesPourCrm(
            MySqlConnection connection, int crmId, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT t.id, t.objet, t.description, t.date_creation,
                       t.crm_id, t.station_id, t.super_admin,
                       CASE WHEN tv.id IS NOT NULL THEN 1 ELSE 0 END AS deja_vu
                FROM ticket t
                LEFT JOIN ticket_vue tv ON t.id = tv.ticket_id AND tv.crm_id = @CrmId
                WHERE t.crm_id = @CrmId";

            if (dateDebut.HasValue) query += " AND t.date_creation >= @DateDebut";
            if (dateFin.HasValue) query += " AND t.date_creation <= @DateFin";

            query += " ORDER BY t.date_creation DESC;";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@CrmId", crmId);
            if (dateDebut.HasValue) cmd.Parameters.AddWithValue("@DateDebut", dateDebut.Value.Date);
            if (dateFin.HasValue) cmd.Parameters.AddWithValue("@DateFin", dateFin.Value.Date);

            var tickets = new List<TicketListItem>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tickets.Add(new TicketListItem
                    {
                        Id = reader.GetInt32("id"),
                        Objet = reader.GetString("objet"),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                        DateCreation = reader.GetDateTime("date_creation"),
                        DejaVu = reader.GetInt32("deja_vu") == 1,
                        Crm = reader.IsDBNull(reader.GetOrdinal("crm_id")) ? null : new Models.Crm.Crm { Id = reader.GetInt32("crm_id") },
                        Station = reader.IsDBNull(reader.GetOrdinal("station_id")) ? null : new Models.Station.Station { Id = reader.GetInt32("station_id") },
                        SuperAdmin = !reader.IsDBNull(reader.GetOrdinal("super_admin")) && reader.GetBoolean(reader.GetOrdinal("super_admin")),
                        CrmDestinataire = new List<Models.Crm.Crm>(),
                        StationDestinataire = new List<Models.Station.Station>(),
                        SuperAdminDestinataire = null
                    });
                }
            }

            LoadDestinatairesGlobal(connection, tickets);

            return tickets;
        }

        private static void LoadDestinatairesGlobal(MySqlConnection connection, List<TicketListItem> tickets)
        {
            if (!tickets.Any()) return;

            var ticketIds = tickets.Select(t => t.Id).ToList();
            string destinatairesQuery = $@"
                SELECT tv.ticket_id, tv.crm_id, tv.station_id, tv.super_admin,
                       c.nom AS crm_nom, s.nom AS station_nom
                FROM ticket_visibilite tv
                LEFT JOIN crm c ON tv.crm_id = c.id
                LEFT JOIN station s ON tv.station_id = s.id
                WHERE tv.ticket_id IN ({string.Join(",", ticketIds)})";

            var dictDest = new Dictionary<int, (List<int> crmIds, List<int> stationIds, bool superAdmin)>();
            using (var cmdDest = new MySqlCommand(destinatairesQuery, connection))
            using (var readerDest = cmdDest.ExecuteReader())
            {
                while (readerDest.Read())
                {
                    int ticketId = readerDest.GetInt32("ticket_id");
                    if (!dictDest.ContainsKey(ticketId))
                        dictDest[ticketId] = (new List<int>(), new List<int>(), false);

                    if (!readerDest.IsDBNull(readerDest.GetOrdinal("crm_id")))
                        dictDest[ticketId].crmIds.Add(readerDest.GetInt32("crm_id"));

                    if (!readerDest.IsDBNull(readerDest.GetOrdinal("station_id")))
                        dictDest[ticketId].stationIds.Add(readerDest.GetInt32("station_id"));

                    if (!readerDest.IsDBNull(readerDest.GetOrdinal("super_admin")) && readerDest.GetBoolean("super_admin"))
                        dictDest[ticketId] = (dictDest[ticketId].crmIds, dictDest[ticketId].stationIds, true);
                }
            } 

            foreach (var t in tickets)
            {
                if (dictDest.TryGetValue(t.Id, out var d))
                {
                    t.CrmDestinataire.AddRange(d.crmIds.Select(id => CrmService.GetCrmById(connection, id)));
                    t.StationDestinataire.AddRange(d.stationIds.Select(id => StationService.GetStationById(connection, id)));
                    t.SuperAdminDestinataire = d.superAdmin;
                }

                if (t.Crm != null)
                    t.Crm = CrmService.GetCrmById(connection, t.Crm.Id);

                if (t.Station != null)
                    t.Station = StationService.GetStationById(connection, t.Station.Id);
            }
        }
    }
}
