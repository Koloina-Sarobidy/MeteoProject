using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Besoin;
using StationControl.Services.Besoin;
using StationControl.Services.Auth;
using StationControl.Services.Intervention;
using StationControl.Models.Auth;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Besoin")]
    public class BesoinController : Controller
    {
        private readonly IConfiguration _configuration;

        public BesoinController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("ListeRenvoye")]
        public IActionResult ListeRenvoye()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            List<BesoinStation> besoinsRenvoyes = BesoinService.GetBesoinRenvoyeSuperAdmin(connection);

            ViewBag.Utilisateur = utilisateur;

            return View(besoinsRenvoyes);
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

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
            {
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });
            }

            if (SelectedBesoins == null || SelectedBesoins.Count == 0)
            {
                TempData["ErrorMessage"] = "Aucun besoin sélectionné.";
                return RedirectToAction("ListeRenvoye");
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

            return RedirectToAction("ListeRenvoye");
        }

        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            var besoins = BesoinService.GetBesoinRenvoyeSuperAdmin(connection);
            var besoin = besoins.FirstOrDefault(b => b.Id == id);

            if (besoin == null)
            {
                TempData["ErrorMessage"] = "Besoin introuvable.";
                return RedirectToAction("ListeRenvoye");
            }

            var intervention = InterventionService.GetInterventionByBesoinId(connection, id);

            ViewBag.Utilisateur = utilisateur;
            ViewBag.Intervention = intervention;

            return View(besoin);
        }

        [HttpGet("DetailBesoin")]
        public IActionResult DetailBesoin(int idEquipement, int idStation)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Utilisateur = utilisateur;

            var besoin = BesoinService.GetBesoinByEquipementStation(connection, idStation, idEquipement);
            int idBesoin = besoin.Id;

            var intervention = InterventionService.GetInterventionByBesoinId(connection, idBesoin);
            ViewBag.Intervention = intervention;
            return View("Details", besoin);
        }

        [HttpPost("AnnulerIntervention/{id}")]
        public IActionResult AnnulerIntervention(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            try
            {
                InterventionService.AnnulerIntervention(connection, id);

                TempData["SuccessMessage"] = "L'intervention a été annulée avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Impossible d'annuler l'intervention : " + ex.Message;
            }

            return RedirectToAction("ListeRenvoye");
        }

    }
}
