using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Services.Besoin;
using StationControl.Models.Auth;
using StationControl.Services.Auth;
using StationControl.Models.Station;
using StationControl.Services.Station;
using StationControl.Areas.SuperAdmin.Controllers;
using StationControl.Services.Intervention;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace StationControl.Areas.Observateur.Controllers
{
    [Area("Observateur")]
    [Route("Observateur/Station")]
    public class StationController : Controller
    {
        private readonly IConfiguration _configuration;

        public StationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Capteurs")]
        public IActionResult Capteurs()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            ViewBag.Utilisateur = utilisateur;
            int id = utilisateur.StationId ?? 0;

            Station station = StationService.GetStationById(connection, id);

            var capteursStation = StationService.GetCapteurByStation(connection, id);

            var capteurs = CapteurService.GetAllCapteur(connection);

            ViewBag.Capteurs = capteurs;
            ViewBag.CapteursStation = capteursStation;
            ViewBag.Station = station;

            return View();
        }


        [HttpPost("AjouterCapteur")]
        public IActionResult AjouterCapteur(CapteurStationInsertRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NumSerie))
            {
                TempData["ErrorMessage"] = "Les données du capteur sont invalides.";
                return RedirectToAction("Capteurs");
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            try
            {
                var capteurStation = new CapteurStation
                {
                    Station = new Station { Id = request.StationId },
                    Capteur = new Models.Equipement.Capteur { Id = request.CapteurId },
                    NumSerie = request.NumSerie.Trim(),
                    DateDebut = request.DateDebut,
                    DateFin = request.DateFin,
                    EstimationVieAnnee = 3,
                    Statut = string.IsNullOrWhiteSpace(request.Statut) ? "Fonctionnel" : request.Statut
                };

                StationService.InsertCapteurStation(connection, capteurStation);
                TempData["SuccessMessage"] = "Capteur ajouté avec succès";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de l'ajout du capteur : {ex.Message}";
            }

            return RedirectToAction("Capteurs");
        }

        [HttpGet("Alimentations")]
        public IActionResult Alimentations()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            ViewBag.Utilisateur = utilisateur;
            int id = utilisateur.StationId ?? 0;

            Station? station = StationService.GetStationById(connection, id);
            if (station == null)
            {
                TempData["ErrorMessage"] = $"La station avec l'ID {id} n'a pas été trouvée.";
                return RedirectToAction("Liste");
            }

            var alimentationsStation = StationService.GetAlimentationByStation(connection, id);

            var alimentations = AlimentationService.GetAllAlimentation(connection);

            ViewBag.Alimentations = alimentations;
            ViewBag.AlimentationsStation = alimentationsStation;
            ViewBag.Station = station;

            return View();
        }

        [HttpPost("AjouterAlimentation")]
        public IActionResult AjouterAlimentation(AlimentationStationInsertRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NumSerie))
            {
                TempData["ErrorMessage"] = "Les données de l'alimentation sont invalides.";
                return RedirectToAction("Capteurs");
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            try
            {
                var alimStation = new AlimentationStation
                {
                    Station = new Station { Id = request.StationId },
                    Alimentation = new Models.Equipement.Alimentation { Id = request.AlimentationId },
                    NumSerie = request.NumSerie.Trim(),
                    DateDebut = request.DateDebut,
                    DateFin = request.DateFin,
                    EstimationVieAnnee = 3,
                    Statut = string.IsNullOrWhiteSpace(request.Statut) ? "Fonctionnel" : request.Statut
                };

                StationService.InsertAlimentationStation(connection, alimStation);
                TempData["SuccessMessage"] = "Alimentation ajoutée avec succès";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de l'ajout de l'alimentation : {ex.Message}";
            }

            return RedirectToAction("Alimentations");
        }

    }
}
