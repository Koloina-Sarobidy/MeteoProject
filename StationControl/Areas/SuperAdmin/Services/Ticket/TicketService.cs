using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using StationControl.Models.Ticket;
using StationControl.Services.Crm;
using StationControl.Services.Station;

namespace StationControl.Areas.SuperAdmin.Services.Ticket
{
    public static class TicketService
    {
        // Tickets envoyés pour SuperAdmin
        public static List<TicketListItem> GetTicketsEnvoyesPourSuperAdmin(
            MySqlConnection connection, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            if (!dateDebut.HasValue && !dateFin.HasValue)
            {
                dateDebut = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                dateFin = DateTime.Now;
            }

            string query = @"
                SELECT t.id, t.objet, t.description, t.date_creation,
                    CASE WHEN tv.id IS NOT NULL THEN 1 ELSE 0 END AS deja_vu,
                    t.crm_id, t.station_id
                FROM ticket t
                LEFT JOIN ticket_vue tv ON t.id = tv.ticket_id AND tv.super_admin = 1
                WHERE t.utilisateur_id IS NOT NULL AND t.crm_id IS NULL AND t.station_id IS NULL";

            if (dateDebut.HasValue)
                query += " AND DATE(t.date_creation) >= DATE(@DateDebut)";
            if (dateFin.HasValue)
                query += " AND DATE(t.date_creation) <= DATE(@DateFin)";

            query += " ORDER BY t.date_creation DESC;";

            using var cmd = new MySqlCommand(query, connection);
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
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) 
                                        ? null 
                                        : reader.GetString("description"),
                        DateCreation = reader.GetDateTime("date_creation"),
                        DejaVu = reader.GetInt32("deja_vu") == 1,
                        Crm = reader.IsDBNull(reader.GetOrdinal("crm_id")) 
                                ? null 
                                : new Models.Crm.Crm { Id = reader.GetInt32("crm_id") },
                        Station = reader.IsDBNull(reader.GetOrdinal("station_id")) 
                                ? null 
                                : new Models.Station.Station { Id = reader.GetInt32("station_id") },
                        CrmDestinataire = new List<Models.Crm.Crm>(),
                        StationDestinataire = new List<Models.Station.Station>()
                    });
                }
            }

            if (!tickets.Any()) return tickets;

            string destinatairesQuery = $@"
                SELECT ticket_id, crm_id, station_id
                FROM ticket_visibilite
                WHERE ticket_id IN ({string.Join(",", tickets.Select(t => t.Id))})";

            var dictDest = new Dictionary<int, (List<int> crmIds, List<int> stationIds)>();
            using (var cmdDest = new MySqlCommand(destinatairesQuery, connection))
            using (var readerDest = cmdDest.ExecuteReader())
            {
                while (readerDest.Read())
                {
                    int ticketId = readerDest.GetInt32("ticket_id");
                    if (!dictDest.ContainsKey(ticketId))
                        dictDest[ticketId] = (new List<int>(), new List<int>());

                    if (!readerDest.IsDBNull(readerDest.GetOrdinal("crm_id")))
                        dictDest[ticketId].crmIds.Add(readerDest.GetInt32("crm_id"));

                    if (!readerDest.IsDBNull(readerDest.GetOrdinal("station_id")))
                        dictDest[ticketId].stationIds.Add(readerDest.GetInt32("station_id"));
                }
            }

            foreach (var t in tickets)
            {
                if (dictDest.TryGetValue(t.Id, out var d))
                {
                    t.CrmDestinataire.AddRange(d.crmIds.Select(id => CrmService.GetCrmById(connection, id)));
                    t.StationDestinataire.AddRange(d.stationIds.Select(id => StationService.GetStationById(connection, id)));
                }

                if (t.Crm != null) t.Crm = CrmService.GetCrmById(connection, t.Crm.Id);
                if (t.Station != null) t.Station = StationService.GetStationById(connection, t.Station.Id);
            }

            return tickets;
        }


        public static List<TicketListItem> GetTicketsRecusPourSuperAdmin(
        MySqlConnection connection, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            if (connection == null)
            throw new ArgumentNullException(nameof(connection));

            if (!dateDebut.HasValue && !dateFin.HasValue)
            {
                dateDebut = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                dateFin = DateTime.Now;
            }

            string query = @"
                SELECT t.id, t.objet, t.description, t.date_creation,
                    CASE WHEN tv.id IS NOT NULL THEN 1 ELSE 0 END AS deja_vu,
                    t.crm_id, t.station_id
                FROM ticket t
                INNER JOIN ticket_visibilite tvs ON t.id = tvs.ticket_id
                LEFT JOIN ticket_vue tv ON t.id = tv.ticket_id AND tv.super_admin = 1
                WHERE tvs.super_admin = 1";

            if (dateDebut.HasValue)
                query += " AND DATE(t.date_creation) >= DATE(@DateDebut)";
            if (dateFin.HasValue)
                query += " AND DATE(t.date_creation) <= DATE(@DateFin)";

            query += " ORDER BY t.date_creation DESC;";

            using var cmd = new MySqlCommand(query, connection);

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
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) 
                                        ? null 
                                        : reader.GetString("description"),
                        DateCreation = reader.GetDateTime("date_creation"),
                        DejaVu = reader.GetInt32("deja_vu") == 1,
                        Crm = reader.IsDBNull(reader.GetOrdinal("crm_id")) 
                                ? null 
                                : new Models.Crm.Crm { Id = reader.GetInt32("crm_id") },
                        Station = reader.IsDBNull(reader.GetOrdinal("station_id")) 
                                ? null 
                                : new Models.Station.Station { Id = reader.GetInt32("station_id") }
                    });
                }
            }

            foreach (var t in tickets)
            {
                if (t.Crm != null) 
                    t.Crm = CrmService.GetCrmById(connection, t.Crm.Id);

                if (t.Station != null) 
                    t.Station = StationService.GetStationById(connection, t.Station.Id);
            }

            return tickets;

        }


        // Tickets non vus pour SuperAdmin
        public static List<TicketListItem> GetTicketsNonVusPourSuperAdmin(MySqlConnection connection, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT t.id, t.objet, t.description, t.date_creation,
                    t.crm_id, t.station_id
                FROM ticket t
                INNER JOIN ticket_visibilite tvs ON t.id = tvs.ticket_id
                LEFT JOIN ticket_vue tv ON t.id = tv.ticket_id AND tv.super_admin = 1
                WHERE tvs.super_admin = 1
                AND tv.id IS NULL"; 

            if (dateDebut.HasValue)
                query += " AND DATE(t.date_creation) >= DATE(@DateDebut)";
            if (dateFin.HasValue)
                query += " AND DATE(t.date_creation) <= DATE(@DateFin)";

            query += " ORDER BY t.date_creation DESC;";

            using var cmd = new MySqlCommand(query, connection);
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
                        Station = reader.IsDBNull(reader.GetOrdinal("station_id")) ? null : new Models.Station.Station { Id = reader.GetInt32("station_id") }
                    });
                }
            }

            foreach (var t in tickets)
            {
                if (t.Crm != null) t.Crm = CrmService.GetCrmById(connection, t.Crm.Id);
                if (t.Station != null) t.Station = StationService.GetStationById(connection, t.Station.Id);
            }

            return tickets;
        }
    }
}
