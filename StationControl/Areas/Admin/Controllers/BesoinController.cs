using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Models.Inventaire;
using StationControl.Models.Station;
using StationControl.Services;
using StationControl.Services.Auth;
using StationControl.Services.Besoin;
using StationControl.Services.Intervention;
using StationControl.Services.Inventaire;
using StationControl.Services.Station;
using System.Linq;

namespace StationControl.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Besoin")]
    public class BesoinController : Controller
    {
        private readonly IConfiguration _configuration;

        public BesoinController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Liste")]
        public IActionResult Liste()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });
            
            int crmId = utilisateur.CrmId ?? 0;
            List<StationControl.Models.Besoin.BesoinStation> besoins = BesoinService.GetBesoinPourCrm(connection, crmId);

            var stations = StationService.GetAllStationByCrm(connection, crmId);
            ViewBag.Stations = stations; 
            ViewBag.Utilisateur = utilisateur;
            return View(besoins);
        }

        [HttpGet("Planifies")]
        public IActionResult Planifies()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });
            
            int crmId = utilisateur.CrmId ?? 0;
            List<Models.Besoin.BesoinStation> besoins = BesoinService.GetBesoinPlanifies(connection, crmId);

            var stations = StationService.GetAllStationByCrm(connection, crmId);

            ViewBag.Stations = stations;
            ViewBag.Utilisateur = utilisateur;
            return View(besoins);
        }

        [HttpPost("RenvoyerSuperAdmin")]
        public IActionResult RenvoyerSuperAdmin(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });

            try
            {
                BesoinService.RenvoyerSuperAdmin(connection, id);

                TempData["SuccessMessage"] = "Le besoin a été renvoyé au Chef SMIT avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors du renvoi du besoin : {ex.Message}";
            }

            return RedirectToAction("Liste", new { area = "Admin" });
        }

        [HttpPost("Planifier")]
        public IActionResult Planifier(
            int StationId,
            DateTime DateDebut,
            DateTime? DateFin,
            string Technicien,
            List<int> SelectedBesoins)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
            {
                return RedirectToAction("Login", "Auth", new { area = "Admin" });
            }

            if (SelectedBesoins == null || SelectedBesoins.Count == 0)
            {
                TempData["ErrorMessage"] = "Aucun besoin sélectionné.";
                return RedirectToAction("Liste");
            }

            try
            {
                var interventions = SelectedBesoins.Select(besoinId => new Models.Intervention.Intervention
                {
                    StationId = StationId,
                    BesoinStationId = besoinId,
                    UtilisateurPlanificationId = utilisateur.Id, 
                    DatePlanifieeDebut = DateDebut,
                    DatePlanifieeFin = DateFin,
                    TechnicienPlanifie = Technicien,
                    Date = DateTime.Now,
                    Statut = "Planifiée"
                }).ToList();

                var insertedIds = InterventionService.PlanifierInterventions(connection, interventions);

                TempData["SuccessMessage"] = $"{insertedIds.Count} intervention(s) enregistrée(s) avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erreur lors de la planification : " + ex.Message;
            }

            return RedirectToAction("Liste");
        }

        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });

            int crmId = utilisateur.CrmId ?? 0;

            var besoins = BesoinService.GetBesoinPourCrm(connection, crmId);
            var besoin = besoins.FirstOrDefault(b => b.Id == id);

            if (besoin == null)
            {
                TempData["ErrorMessage"] = "Besoin introuvable.";
                return RedirectToAction("Liste");
            }

            var intervention = InterventionService.GetInterventionByBesoinId(connection, id);

            ViewBag.Utilisateur = utilisateur;
            ViewBag.Intervention = intervention;

            var stations = StationService.GetAllStationByCrm(connection, crmId);
            ViewBag.Stations = stations;
            return View(besoin);
        }

        [HttpPost("AnnulerIntervention/{id}")]
        public IActionResult AnnulerIntervention(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });

            try
            {
                InterventionService.AnnulerIntervention(connection, id);

                TempData["SuccessMessage"] = "L'intervention a été annulée avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Impossible d'annuler l'intervention : " + ex.Message;
            }

            return RedirectToAction("Liste");
        }
    }
}
