using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Ticket;
using StationControl.Models.Auth;
using StationControl.Services.Auth;
using StationControl.Services.Ticket;
using StationControl.Services.Crm;
using StationControl.Services.Station;
using System.Linq;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Ticket")]
    public class TicketController : Controller
    {
        private readonly IConfiguration _configuration;

        public TicketController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Crms = CrmService.GetAllCrms(connection);
            ViewBag.Stations = StationService.GetAllStation(connection);
            ViewBag.Utilisateur = utilisateur;

            return View("Ticket");
        }

        [HttpPost("CreateTicket")]
        public IActionResult CreateTicket([FromForm] Ticket ticket, [FromForm] List<int> CrmIds, [FromForm] List<int> StationIds)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            try
            {
                ticket.UtilisateurId = utilisateur.Id;
                ticket.DateCreation = DateTime.Now;
                ticket.SuperAdmin = true;

                if (CrmIds != null)
                {
                    foreach (var crmId in CrmIds.Distinct())
                    {
                        ticket.Visibilites.Add(new TicketVisibilite { CrmId = crmId });
                    }
                }

                if (StationIds != null)
                {
                    foreach (var stationId in StationIds.Distinct())
                    {
                        ticket.Visibilites.Add(new TicketVisibilite { StationId = stationId });
                    }
                }

                foreach (var file in Request.Form.Files)
                {
                    string path = TicketService.SaveFile(file, "tickets");
                    ticket.PiecesJointes.Add(new TicketPieceJointe { Url = path });
                }

                int ticketId = TicketService.CreerTicket(connection, ticket);

                TempData["SuccessMessage"] = "Ticket créé avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la création du ticket : {ex.Message}";
            }
            return RedirectToAction("Create");
        }

        [HttpGet("ListeTicket")]
        public IActionResult ListeTicket(DateTime? dateDebut, DateTime? dateFin)
        {
            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            var superAdmin = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (superAdmin == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            var ticketsEnvoyes = Services.Ticket.TicketService.GetTicketsEnvoyesPourSuperAdmin(connection, dateDebut, dateFin);
            var ticketsRecus = Services.Ticket.TicketService.GetTicketsRecusPourSuperAdmin(connection, dateDebut, dateFin);

            var model = new TicketsSuperAdminViewModel
            {
                DateDebut = dateDebut,
                DateFin = dateFin,
                TicketsEnvoyes = ticketsEnvoyes,
                TicketsRecus = ticketsRecus
            };
            ViewBag.Utilisateur = superAdmin;

            return View(model);
        }

        [HttpGet("DetailEnvoye/{ticketId}")]
        public IActionResult DetailEnvoye(int ticketId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var superAdmin = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (superAdmin == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            var ticket = TicketService.GetTicketListItemById(connection, ticketId, null, null, true);
            if (ticket == null)
                return NotFound("Ticket introuvable");

            ViewBag.PiecesJointes = TicketService.GetTicketAttachments(connection, ticketId);

            var vues = TicketService.GetVu(connection, ticketId);

            ViewBag.Vues = vues;
            ViewBag.Utilisateur = superAdmin;

            return View("DetailEnvoye", ticket); 
        }

        [HttpGet("DetailRecu/{ticketId}")]
        public IActionResult DetailRecu(int ticketId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var superAdmin = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (superAdmin == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            var ticket = StationControl.Services.Ticket.TicketService.GetTicketListItemById(connection, ticketId, null, null, true);
            if (ticket == null)
                return NotFound("Ticket introuvable");
            
            if(ticket.DejaVu != true)
            {
                TicketService.InsertTicketVue(connection, ticketId, true, null, null);
                ticket.DejaVu = true;
            }

            ViewBag.PiecesJointes = TicketService.GetTicketAttachments(connection, ticketId);

            var vues = TicketService.GetVu(connection, ticketId);

            ViewBag.Vues = vues;
            ViewBag.Utilisateur = superAdmin;

            return View("DetailRecu", ticket); 
        }

        [HttpGet("Download/{fileName}")]
        public IActionResult Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("Nom de fichier invalide");

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tickets", fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound("Fichier introuvable");

            var contentType = "application/octet-stream"; 
            return PhysicalFile(filePath, contentType, fileName);
        }
    }
}
