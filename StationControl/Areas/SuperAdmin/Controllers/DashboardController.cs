using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Services;
using StationControl.Services.Auth;
using StationControl.Services.Station;
using StationControl.Services.Crm;
using StationControl.Services.Intervention;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Dashboard")]
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

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Utilisateur = utilisateur;

            return View();
        }

        [HttpGet("RealtimeData")]
        public IActionResult RealtimeData()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            // ===== Stations =====
            var stations = StationService.GetAllStation(connection)
                .Select(s => new
                {
                    s.Id,
                    Nom = s.Nom,
                    Statut = s.Statut,
                    Latitude = s.Latitude?.ToString() ?? "",
                    Longitude = s.Longitude?.ToString() ?? "",
                    TypeStation = s.TypeStation != null ? new { s.TypeStation.Libelle } : null,
                    Region = s.Region != null ? new { s.Region.Libelle } : null,
                    DateDebut = s.DateDebut,
                    Crm = s.Crm
                }).ToList();

            // ===== CRM Charts =====
            var crmList = CrmService.GetAllCrms(connection);
            var crmIds = crmList.Select(c => c.Id).ToList();
            var stationPercentages = StationService.GetStationStatusPercentageByCrm(connection, crmIds);

            var crmChartData = crmList.Select(c => new
            {
                CrmId = c.Id,
                CrmName = c.Libelle,
                Percentages = stationPercentages.ContainsKey(c.Id)
                    ? stationPercentages[c.Id]
                    : new Dictionary<string, double>
                    {
                        { "Fonctionnelle", 0 },
                        { "Partiellement Fonctionnelle", 0 },
                        { "Non Fonctionnelle", 0 }
                    }
            }).ToList();

            // ===== Interventions =====
            var interventionStats = InterventionService.GetCurativeInterventionCountByStation(connection);

            // ===== Brand Stats =====
            var brandStats = StationService.GetStatutBrandStats(connection);


            return Json(new
            {
                Stations = stations,
                CrmChartData = crmChartData,
                InterventionStats = interventionStats,
                BrandStats = brandStats
            });

        }
        
    }
}
