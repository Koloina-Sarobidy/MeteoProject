using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Services.Auth;
using StationControl.Models.Auth;
using StationControl.Services.Station;
using StationControl.Models.Personnel;
using StationControl.Services.Crm;

namespace StationControl.Areas.Observateur.Controllers
{
    [Area("Observateur")]
    [Route("Observateur/Personnel")]
    public class PersonnelController : Controller
    {
        private readonly IConfiguration _configuration;

        public PersonnelController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Liste")]
        public IActionResult Liste(DateTime? dateDebut, string statut)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            ViewBag.FiltreDateDebut = dateDebut;
            ViewBag.FiltreStatut = statut;

            int stationId = utilisateur.StationId ?? 0;
            ViewBag.Utilisateur = utilisateur;

            var observateurs = UtilisateurService.GetObservateursFiltres(connection, stationId, dateDebut, statut);

            return View(observateurs);
        }

        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? superAdmin = AuthService.GetObservateurFromCookie(Request, connection);
            if (superAdmin == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            Utilisateur observateur = UtilisateurService.GetUtilisateurById(connection, id);
            if (observateur == null)
            {
                TempData["ErrorMessage"] = "Détails Observateur introuvable";
                return RedirectToAction("Liste");
            }

            int idObservateur = observateur.Id;
            var statut = UtilisateurStatutHistoriqueService.GetLastStatutByUtilisateur(connection, idObservateur);

            int idStation = observateur.StationId ?? 0;
            var station = StationService.GetStationById(connection, idStation);
            ViewBag.Station = station;

            ViewBag.Statut = statut;
            ViewBag.Utilisateur = superAdmin;
            return View(observateur);
        }

        [HttpPost("ModifierStatut")]
        public IActionResult ModifierStatut(string statut, string? description, DateTime dateDebut, DateTime? dateFin, int utilisateurId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var superAdmin = AuthService.GetObservateurFromCookie(Request, connection);
            if (superAdmin == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });
           
            Console.WriteLine(utilisateurId);

            UtilisateurService.ModifierStatut(connection, utilisateurId, statut, superAdmin.Id, description, dateDebut, dateFin);

            TempData["SuccessMessage"] = "Statut mis à jour avec succès.";

            return RedirectToAction("Details", new { id = utilisateurId });
        }

        [HttpGet("Inscription")]
        public IActionResult Inscription()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            int idStation = utilisateur.StationId.Value;
            var model = new DemandesInscriptionViewModel
            {
                DemandesObservateurs = UtilisateurService.GetObservateursNonValidesByStation(connection, idStation),
                Stations = StationService.GetAllStation(connection).ToDictionary(s => s.Id, s => s.Nom)
            };

            ViewBag.Utilisateur = utilisateur;

            return View(model);
        }

        [HttpPost("Valider")]
        public IActionResult Valider(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            try
            {
                UtilisateurService.ValiderUtilisateur(connection, id);
                TempData["SuccessMessage"] = "Utilisateur validé avec succès !";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la validation de l'utilisateur: {ex.Message}";
            }

            var utilisateurValid = UtilisateurService.GetUtilisateurById(connection, id);
            EmailService.SendEmail(_configuration, utilisateurValid, "Validation Inscription", "Votre demande d'inscription dans StationControl est validée.");
            return RedirectToAction("Inscription");
        }

        [HttpPost("Supprimer")]
        public IActionResult Supprimer(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? superAdmin = AuthService.GetObservateurFromCookie(Request, connection);
            if (superAdmin == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            try
            {
                bool success = UtilisateurService.DeleteUtilisateur(connection, id);

                if (success)
                    TempData["SuccessMessage"] = "Utilisateur supprimé avec succès.";
                else
                    TempData["ErrorMessage"] = "Impossible de supprimer l'utilisateur : utilisateur introuvable.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la suppression : {ex.Message}";
            }
            return RedirectToAction("Inscription");
        }
    }
}
