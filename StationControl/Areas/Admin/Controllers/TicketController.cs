using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Ticket;
using StationControl.Models.Auth;
using StationControl.Services.Auth;
using StationControl.Areas.Admin.Services.Ticket;
using StationControl.Services.Crm;
using StationControl.Services.Station;

namespace StationControl.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Ticket")]
    public class TicketController : Controller
    {
        private readonly IConfiguration _configuration;

        public TicketController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("ListeTicket")]
        public IActionResult ListeTicket(DateTime? dateDebut, DateTime? dateFin)
        {
            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            var observateur = AuthService.GetAdminFromCookie(Request, connection);
            if (observateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });

            var ticketsEnvoyes = TicketService.GetTicketsEnvoyesPourCrm(
                connection, observateur.CrmId ?? 0, dateDebut, dateFin
            );

            var ticketsRecus = TicketService.GetTicketsRecusPourCrm(
                connection, observateur.CrmId ?? 0, dateDebut, dateFin
            );

            var model = new TicketsSuperAdminViewModel
            {
                DateDebut = dateDebut,
                DateFin = dateFin,
                TicketsEnvoyes = ticketsEnvoyes,
                TicketsRecus = ticketsRecus
            };

            ViewBag.Utilisateur = observateur;
            return View(model);
        }

        [HttpGet("Details")]
        public IActionResult Details(int ticketId, bool isRecus)
        {
            
            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            var observateur = AuthService.GetAdminFromCookie(Request, connection);
            if (observateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });

            var ticket = StationControl.Services.Ticket.TicketService.GetTicketListItemById(connection, ticketId, null, observateur.CrmId, null);
            if (ticket == null)
                return NotFound("Ticket introuvable");

            Console.WriteLine("=== DEBUG ===");
            Console.WriteLine("isRecus = " + isRecus);
            Console.WriteLine("ticket.DejaVu = " + ticket.DejaVu);
            Console.WriteLine("Résultat du IF = " + (isRecus && !ticket.DejaVu));
            if (isRecus && !ticket.DejaVu)
            {
                StationControl.Services.Ticket.TicketService.InsertTicketVue(connection, ticketId, null, observateur.CrmId, null);
                ticket.DejaVu = true;
            }

            ViewBag.PiecesJointes = StationControl.Services.Ticket.TicketService.GetTicketAttachments(connection, ticketId);
            ViewBag.Vues = StationControl.Services.Ticket.TicketService.GetVu(connection, ticketId);
            ViewBag.Utilisateur = observateur;

            return View(isRecus ? "DetailRecu" : "DetailEnvoye", ticket);
        }

        [HttpGet("Download/{fileName}")]
        public IActionResult Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("Nom de fichier invalide");

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "uploads", "tickets", fileName
            );

            if (!System.IO.File.Exists(filePath))
                return NotFound("Fichier introuvable");

            return PhysicalFile(filePath, "application/octet-stream", fileName);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });

            ViewBag.Crms = CrmService.GetAllCrms(connection);
            ViewBag.Stations = StationService.GetAllStation(connection);
            ViewBag.Utilisateur = utilisateur;

            return View("Ticket");
        }

        [HttpPost("CreateTicket")]
        public IActionResult CreateTicket([FromForm] Ticket ticket, [FromForm] List<int> CrmIds, [FromForm] List<int> StationIds, [FromForm] bool? superadmin)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });

            try
            {
                ticket.UtilisateurId = utilisateur.Id;
                ticket.CrmId = utilisateur.CrmId;
                ticket.DateCreation = DateTime.Now;
                ticket.SuperAdmin = false;

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

                if (superadmin == true)
                {
                    ticket.Visibilites.Add(new TicketVisibilite { SuperAdmin = true });
                }


                foreach (var file in Request.Form.Files)
                {
                    string path = StationControl.Services.Ticket.TicketService.SaveFile(file, "tickets");
                    ticket.PiecesJointes.Add(new TicketPieceJointe { Url = path });
                }

                int ticketId = StationControl.Services.Ticket.TicketService.CreerTicket(connection, ticket);

                TempData["SuccessMessage"] = "Ticket créé avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la création du ticket : {ex.Message}";
            }
            return RedirectToAction("Create");
        }
    }
}


