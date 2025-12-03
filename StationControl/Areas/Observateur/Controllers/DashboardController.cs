using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Services;
using StationControl.Models.Auth;
using StationControl.Services.Auth;
using StationControl.Services.Station;
using StationControl.Services.Besoin;
using StationControl.Services.Util;

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

            var utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            int stationId = utilisateur.StationId ?? 0;

            ViewBag.Station = StationService.GetStationById(connection, stationId);
            ViewBag.CapteursStation = StationService.GetCapteurByStation(connection, stationId);
            ViewBag.AlimentationsStation = StationService.GetAlimentationByStation(connection, stationId);
            ViewBag.HistoriqueBesoins = BesoinService.GetHistoriqueBesoinsAnneeEnCours(connection, stationId);
            ViewBag.KpiStation = KpiStationService.GetKpiStation(connection, stationId);
            ViewBag.Utilisateur = utilisateur;

            return View();
        }

        [HttpGet("RealtimeData")]
        public IActionResult RealtimeData()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return Unauthorized();

            int stationId = utilisateur.StationId ?? 0;

            var station = StationService.GetStationById(connection, stationId);
            var capteursStation = StationService.GetCapteurByStation(connection, stationId);
            var alimentationsStation = StationService.GetAlimentationByStation(connection, stationId);
            var historiqueBesoins = BesoinService.GetHistoriqueBesoinsAnneeEnCours(connection, stationId);
            var kpiStation = KpiStationService.GetKpiStation(connection, stationId);

            return new JsonResult(new
            {
                Station = station,
                CapteursStation = capteursStation,
                AlimentationsStation = alimentationsStation,
                HistoriqueBesoins = historiqueBesoins,
                KpiStation = kpiStation
            });
        }

    }
}
