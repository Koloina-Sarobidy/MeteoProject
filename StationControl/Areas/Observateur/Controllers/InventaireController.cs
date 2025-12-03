using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Models.Station;
using StationControl.Models.Inventaire;
using StationControl.Services.Station;
using StationControl.Services.Auth;
using System.Collections.Generic;
using System.Text.Json;
using StationControl.Services.Inventaire;

namespace StationControl.Areas.Observateur.Controllers
{
    [Area("Observateur")]
    [Route("Observateur/Inventaire")]
    public class InventaireController : Controller
    {
        private readonly IConfiguration _configuration;

        public InventaireController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Inventaire")]
        public IActionResult Inventaire()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            int stationId = utilisateur.StationId ?? 0;
            Station? station = StationService.GetStationById(connection, stationId);

            var capteursStation = StationService.GetCapteurByStation(connection, stationId);
            var alimentationsStation = StationService.GetAlimentationByStation(connection, stationId);
            var equipementsBesoin = StationService.GetAllEquipementBesoin(connection);

            ViewBag.Station = station;
            ViewBag.CapteursStation = capteursStation;
            ViewBag.AlimentationsStation = alimentationsStation;
            ViewBag.EquipementsBesoin = equipementsBesoin;
            ViewBag.Utilisateur = utilisateur;

            return View("Ajout");
        }

        [HttpPost]
        public IActionResult EnregistrerInventaire(Inventaire inventaire)
        {
            if (inventaire == null || inventaire.Details == null || inventaire.Details.Count == 0)
            {
                TempData["ErrorMessage"] = "Les détails sont vides.";
                return RedirectToAction("Inventaire");
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                var utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
                if (utilisateur == null)
                    return RedirectToAction("Login", "Auth", new { area = "Observateur" });

                inventaire.UtilisateurId = utilisateur.Id;

                bool resultat = InventaireService.InsererInventaire(
                    connection,
                    inventaire.StationId,
                    inventaire.UtilisateurId,
                    inventaire.Details,
                    inventaire.Commentaire
                );

                TempData["SuccessMessage"] = resultat
                    ? "Inventaire sauvegardé avec succès."
                    : "Erreur lors de la sauvegarde de l'inventaire.";

                return RedirectToAction("Dashboard", "Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la sauvegarde de l'inventaire : {ex.Message}";
                return RedirectToAction("Inventaire");
            }
        }

        [HttpGet("GetPrediction")]
        public IActionResult GetPrediction(int equipementStationId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var pred = PredictionService.GetPredictionsParTypeByEquipementStation(connection, equipementStationId);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            };

            return new JsonResult(pred, options);
        }

    }
}
