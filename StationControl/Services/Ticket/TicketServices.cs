using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using StationControl.Models.Ticket;
using StationControl.Services.Crm;
using StationControl.Services.Station;

namespace StationControl.Services.Ticket
{
    public static class TicketService
    {
        public static int CreerTicket(MySqlConnection connection, Models.Ticket.Ticket ticket)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));

            using var transaction = connection.BeginTransaction();
            try
            {
                string insertTicket = @"
                    INSERT INTO ticket (objet, description, date_creation, utilisateur_id, crm_id, station_id, super_admin)
                    VALUES (@Objet, @Description, @DateCreation, @UtilisateurId, @CrmId, @StationId, @SuperAdmin);
                    SELECT LAST_INSERT_ID();";

                using var cmdTicket = new MySqlCommand(insertTicket, connection, transaction);
                cmdTicket.Parameters.AddWithValue("@Objet", ticket.Objet);
                cmdTicket.Parameters.AddWithValue("@Description", ticket.Description ?? (object)DBNull.Value);
                cmdTicket.Parameters.AddWithValue("@DateCreation", ticket.DateCreation);
                cmdTicket.Parameters.AddWithValue("@UtilisateurId", ticket.UtilisateurId);
                cmdTicket.Parameters.AddWithValue("@CrmId", ticket.CrmId.HasValue ? (object)ticket.CrmId.Value : DBNull.Value);
                cmdTicket.Parameters.AddWithValue("@StationId", ticket.StationId.HasValue ? (object)ticket.StationId.Value : DBNull.Value);
                cmdTicket.Parameters.AddWithValue("@SuperAdmin", ticket.SuperAdmin.HasValue ? (object)ticket.SuperAdmin.Value : DBNull.Value);

                int ticketId = Convert.ToInt32(cmdTicket.ExecuteScalar());
                ticket.Id = ticketId;

                foreach (var piece in ticket.PiecesJointes)
                {
                    string insertPiece = "INSERT INTO ticket_piece_jointe (ticket_id, url) VALUES (@TicketId, @Url)";
                    using var cmdPiece = new MySqlCommand(insertPiece, connection, transaction);
                    cmdPiece.Parameters.AddWithValue("@TicketId", ticketId);
                    cmdPiece.Parameters.AddWithValue("@Url", piece.Url ?? (object)DBNull.Value);
                    cmdPiece.ExecuteNonQuery();
                }

                foreach (var visibilite in ticket.Visibilites)
                {
                    string insertVisibilite = @"
                        INSERT INTO ticket_visibilite (ticket_id, super_admin, crm_id, station_id)
                        VALUES (@TicketId, @SuperAdmin, @CrmId, @StationId)";
                    using var cmdVisibilite = new MySqlCommand(insertVisibilite, connection, transaction);
                    cmdVisibilite.Parameters.AddWithValue("@TicketId", ticketId);
                    cmdVisibilite.Parameters.AddWithValue("@SuperAdmin", visibilite.SuperAdmin.HasValue ? (object)visibilite.SuperAdmin.Value : DBNull.Value);
                    cmdVisibilite.Parameters.AddWithValue("@CrmId", visibilite.CrmId.HasValue ? (object)visibilite.CrmId.Value : DBNull.Value);
                    cmdVisibilite.Parameters.AddWithValue("@StationId", visibilite.StationId.HasValue ? (object)visibilite.StationId.Value : DBNull.Value);
                    cmdVisibilite.ExecuteNonQuery();
                }

                transaction.Commit();
                return ticketId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public static string SaveFile(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Fichier invalide");

            string uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folderName);
            if (!Directory.Exists(uploadsRoot))
                Directory.CreateDirectory(uploadsRoot);

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string fullPath = Path.Combine(uploadsRoot, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
                file.CopyTo(stream);

            return uniqueFileName;
        }

        public static void AddTicketAttachments(MySqlConnection connection, int ticketId, IEnumerable<IFormFile> files)
        {
            foreach (var file in files)
            {
                string url = SaveFile(file, "tickets");
                string insertPiece = "INSERT INTO ticket_piece_jointe (ticket_id, url) VALUES (@TicketId, @Url)";
                using var cmd = new MySqlCommand(insertPiece, connection);
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                cmd.Parameters.AddWithValue("@Url", url);
                cmd.ExecuteNonQuery();
            }
        }

        public static TicketListItem GetTicketById(MySqlConnection connection, int ticketId)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            TicketListItem ticket = null;

            string query = @"
                SELECT t.id, t.objet, t.description, t.date_creation,
                    t.crm_id, t.station_id
                FROM ticket t
                WHERE t.id = @TicketId";

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@TicketId", ticketId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        ticket = new TicketListItem
                        {
                            Id = reader.GetInt32("id"),
                            Objet = reader.GetString("objet"),
                            Description = reader.IsDBNull(reader.GetOrdinal("description"))
                                            ? null
                                            : reader.GetString("description"),
                            DateCreation = reader.GetDateTime("date_creation"),
                            Crm = reader.IsDBNull(reader.GetOrdinal("crm_id"))
                                    ? null
                                    : new Models.Crm.Crm { Id = reader.GetInt32("crm_id") },
                            Station = reader.IsDBNull(reader.GetOrdinal("station_id"))
                                        ? null
                                        : new Models.Station.Station { Id = reader.GetInt32("station_id") }
                        };
                    }
                }
            }

            if (ticket != null)
            {
                if (ticket.Crm != null)
                {
                    ticket.Crm = CrmService.GetCrmById(connection, ticket.Crm.Id);
                }

                if (ticket.Station != null)
                {
                    ticket.Station = StationService.GetStationById(connection, ticket.Station.Id);
                }
            }

            return ticket;
        }
        public static List<TicketPieceJointe> GetTicketAttachments(MySqlConnection connection, int ticketId)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var pieces = new List<TicketPieceJointe>();

            string query = @"
                SELECT id, ticket_id, url
                FROM ticket_piece_jointe
                WHERE ticket_id = @TicketId";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TicketId", ticketId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                pieces.Add(new TicketPieceJointe
                {
                    Id = reader.GetInt32("id"),
                    TicketId = reader.GetInt32("ticket_id"),
                    Url = reader.GetString("url")
                });
            }

            return pieces;
        }

        public static TicketListItem GetTicketListItemById(
            MySqlConnection connection, 
            int ticketId,
            int? stationId,
            int? crmId,
            bool? superAdmin
        )
        {
            if (connection == null) 
                throw new ArgumentNullException(nameof(connection));

            TicketListItem ticket = null;

            // Récupération du ticket
            string ticketQuery = @"
                SELECT id, objet, description, date_creation, crm_id, station_id, super_admin
                FROM ticket
                WHERE id = @TicketId";

            using (var cmd = new MySqlCommand(ticketQuery, connection))
            {
                cmd.Parameters.Add("@TicketId", MySqlDbType.Int32).Value = ticketId;

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    ticket = new TicketListItem
                    {
                        Id = reader.GetInt32("id"),
                        Objet = reader.GetString("objet"),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) 
                                        ? null 
                                        : reader.GetString("description"),
                        DateCreation = reader.GetDateTime("date_creation"),
                        Crm = !reader.IsDBNull(reader.GetOrdinal("crm_id")) 
                                ? new Models.Crm.Crm { Id = reader.GetInt32("crm_id") } 
                                : null,
                        Station = !reader.IsDBNull(reader.GetOrdinal("station_id")) 
                                ? new Models.Station.Station { Id = reader.GetInt32("station_id") } 
                                : null,
                        SuperAdmin = reader.GetBoolean("super_admin"),
                        DejaVu = false
                    };
                }
            }

            if (ticket == null)
                return null;

            // Vérification si le ticket a déjà été vu par cet utilisateur/CRM/station
            string vuesQuery = @"
                SELECT COUNT(*)
                FROM ticket_vue
                WHERE ticket_id = @TicketId
                AND (
                    (@StationId IS NOT NULL AND station_id = @StationId)
                    OR (@CrmId IS NOT NULL AND crm_id = @CrmId)
                    OR (@SuperAdmin = 1 AND super_admin = 1)
                )";

            using (var cmd = new MySqlCommand(vuesQuery, connection))
            {
                cmd.Parameters.Add("@TicketId", MySqlDbType.Int32).Value = ticketId;
                cmd.Parameters.Add("@StationId", MySqlDbType.Int32).Value = stationId.HasValue ? (object)stationId.Value : DBNull.Value;
                cmd.Parameters.Add("@CrmId", MySqlDbType.Int32).Value = crmId.HasValue ? (object)crmId.Value : DBNull.Value;
                cmd.Parameters.Add("@SuperAdmin", MySqlDbType.Bit).Value = superAdmin.HasValue ? (object)superAdmin.Value : DBNull.Value;

                ticket.DejaVu = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }

            return ticket;
        }


        public static List<TicketVue> GetVu(MySqlConnection connection, int ticketId)
        {
            if (connection == null)
            throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT id, ticket_id, super_admin, crm_id, station_id, date_vue
                FROM ticket_vue
                WHERE ticket_id = @TicketId
                ORDER BY date_vue ASC;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TicketId", ticketId);

            var tempList = new List<NewStruct>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var vue = new TicketVue
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    TicketId = reader.GetInt32(reader.GetOrdinal("ticket_id")),
                    SuperAdmin = reader.IsDBNull(reader.GetOrdinal("super_admin"))
                        ? (bool?)null
                        : reader.GetBoolean(reader.GetOrdinal("super_admin")),
                    DateVue = reader.IsDBNull(reader.GetOrdinal("date_vue"))
                        ? DateTime.MinValue
                        : reader.GetDateTime(reader.GetOrdinal("date_vue"))
                };

                int? crmId = reader.IsDBNull(reader.GetOrdinal("crm_id"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("crm_id"));

                int? stationId = reader.IsDBNull(reader.GetOrdinal("station_id"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("station_id"));

                tempList.Add(new NewStruct(vue, crmId, stationId));
            }
            reader.Close();

            foreach (var entry in tempList)
            {
                if (entry.crmId.HasValue)
                    entry.vue.CrmId = CrmService.GetCrmById(connection, entry.crmId.Value);

                if (entry.stationId.HasValue)
                    entry.vue.StationId = StationService.GetStationById(connection, entry.stationId.Value);
            }

                return tempList.Select(x => x.vue).ToList();

        }

        public static void InsertTicketVue(
            MySqlConnection connection,
            int ticketId,
            bool? superAdmin = null,
            int? crmId = null,
            int? stationId = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            string query = @"
                INSERT INTO ticket_vue (ticket_id, super_admin, crm_id, station_id, date_vue)
                VALUES (@TicketId, @SuperAdmin, @CrmId, @StationId, NOW())";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TicketId", ticketId);
            cmd.Parameters.AddWithValue("@SuperAdmin", superAdmin.HasValue ? (object)superAdmin.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@CrmId", crmId.HasValue ? (object)crmId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@StationId", stationId.HasValue ? (object)stationId.Value : DBNull.Value);

            cmd.ExecuteNonQuery();
        }

    }

    internal record struct NewStruct(TicketVue vue, int? crmId, int? stationId)
    {
        public static implicit operator (TicketVue vue, int? crmId, int? stationId)(NewStruct value)
        {
            return (value.vue, value.crmId, value.stationId);
        }

        public static implicit operator NewStruct((TicketVue vue, int? crmId, int? stationId) value)
        {
            return new NewStruct(value.vue, value.crmId, value.stationId);
        }
    }
}
