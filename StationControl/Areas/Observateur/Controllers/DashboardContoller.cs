using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Services;
using StationControl.Models.Auth;
using StationControl.Services.Auth;
using StationControl.Services.Station;
using StationControl.Services.Besoin;

namespace StationControl.Areas.Observateur.Controllers
{
    [Area("Observateur")]
    [Route("Observateur/Dashboard")]
    public class DashboardController : Controller
    {
        private readonly IConfiguration _configuration;

        public DashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            ViewBag.Utilisateur = utilisateur;
            int stationId = utilisateur.StationId ?? 0;

            var station = StationService.GetStationById(connection, stationId);
            var capteursStation = StationService.GetCapteurByStation(connection, stationId);
            var alimentationsStation = StationService.GetAlimentationByStation(connection, stationId);
            var historiqueBesoins = BesoinService.GetHistoriqueBesoinsAnneeEnCours(connection, stationId);

            ViewBag.Station = station;
            ViewBag.CapteursStation = capteursStation;
            ViewBag.AlimentationsStation = alimentationsStation;
            ViewBag.HistoriqueBesoins = historiqueBesoins;

            return View();
        }

        [HttpGet("RealtimeData")]
        public IActionResult RealtimeData()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return Unauthorized();

            int stationId = utilisateur.StationId ?? 0;

            var station = StationService.GetStationById(connection, stationId);
            var capteursStation = StationService.GetCapteurByStation(connection, stationId);
            var alimentationsStation = StationService.GetAlimentationByStation(connection, stationId);
            var historiqueBesoins = BesoinService.GetHistoriqueBesoinsAnneeEnCours(connection, stationId);

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null };

            return new JsonResult(new
            {
                Station = station,
                CapteursStation = capteursStation,
                AlimentationsStation = alimentationsStation,
                HistoriqueBesoins = historiqueBesoins
            }, options);
        }
    }
}
