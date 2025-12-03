using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Intervention;
using StationControl.Services.Auth;
using StationControl.Services.Intervention;
using StationControl.Services.Station;

namespace StationControl.Areas.Observateur.Controllers
{
    [Area("Observateur")]
    [Route("Observateur/Preventive")]
    public class PreventiveController : Controller
    {
        private readonly IConfiguration _configuration;

        public PreventiveController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Planifier()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var observateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (observateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            int stationId = observateur.StationId ?? 0;

            var effectifs = UtilisateurService.GetUtilisateurByStation(connection, stationId);

            var model = new PreventiveViewModel
            {
                Effectifs = effectifs,
                DatePrevueDebut = DateTime.Today
            };

            ViewBag.Utilisateur = observateur;

            return View("Preventive", model);
        }

        [HttpPost]
        public IActionResult Planifier(int? UtilisateurEffectifId, DateTime DatePrevueDebut, DateTime? DatePrevueFin, string Description)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var observateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (observateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            int idStation = observateur.StationId ?? 0;

            var preventive = new Preventive
            {
                UtilisateurPlanificateur = observateur,
                UtilisateurEffectif = UtilisateurEffectifId.HasValue ? UtilisateurService.GetUtilisateurById(connection, UtilisateurEffectifId.Value) : null,
                Station = StationService.GetStationById(connection, idStation),
                DatePrevueDebut = DatePrevueDebut,
                DatePrevueFin = DatePrevueFin,
                Description = Description,
                EstComplete = false,
                EstAnnule = false,
                DateEffectiveDebut = null,
                DateEffectiveFin = null
            };

            ViewBag.Utilisateur = observateur;

            PreventiveService.InsertPreventive(connection, preventive);

            TempData["SuccessMessage"] = "Intervention préventive planifiée avec succès !";
            return RedirectToAction("Planifier");
        }

        [HttpGet("Liste")]
        public IActionResult Liste(
            DateTime? dateDebut = null,
            DateTime? dateFin = null,
            int page = 1,
            int pageSize = 10)  
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var observateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (observateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            int stationId = observateur.StationId ?? 0;

            var allPreventives = PreventiveService.GetPreventiveByStation(connection, stationId, dateDebut, dateFin);

            int totalItems = allPreventives.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var pagedPreventives = allPreventives
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList();

            ViewBag.StationNom = StationService.GetStationById(connection, stationId).Nom;
            ViewData["dateDebut"] = dateDebut;
            ViewData["dateFin"] = dateFin;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.Utilisateur = observateur;

            return View("Liste", pagedPreventives);
        }

        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var observateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (observateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            var preventive = PreventiveService.GetPreventiveById(connection, id);
            if (preventive == null)
                return NotFound();

            ViewBag.Utilisateur = observateur;

            int idStation = observateur.StationId ?? 0;
            var utilisateurs = UtilisateurService.GetUtilisateurByStation(connection, idStation);

            ViewBag.Utilisateurs = utilisateurs;

            return View(preventive);
        }

        [HttpPost("Effectuer")]
        public IActionResult Effectuer(int id, int executeurId, DateTime dateDebut, DateTime? dateFin)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var observateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (observateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            try
            {
                PreventiveService.EffectuerPreventive(
                    connection,
                    id,
                    executeurId,
                    dateDebut,
                    dateFin
                );

                TempData["SuccessMessage"] = "Intervention effectuée avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erreur lors de l'enregistrement : " + ex.Message;
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost("Annuler/{id}")]
        public IActionResult Annuler(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var observateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (observateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            try
            {
                PreventiveService.AnnulerPreventive(connection, id);
                TempData["SuccessMessage"] = "Intervention annulée avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erreur lors de l'annulation : " + ex.Message;
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost("Terminer/{id}")]
        public IActionResult Terminer(int id, DateTime dateFin)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var observateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (observateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            var preventive = PreventiveService.GetPreventiveById(connection, id);
            if (preventive == null)
                return NotFound();

            try
            {
                PreventiveService.TerminerPreventive(connection, id, dateFin);
                TempData["SuccessMessage"] = "Intervention terminée avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erreur lors de la finalisation : " + ex.Message;
            }

            return RedirectToAction("Details", new { id });
}

    }
}
