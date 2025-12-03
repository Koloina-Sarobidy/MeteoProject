using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Services;
using StationControl.Models.Auth;
using StationControl.Services.Auth;
using StationControl.Services.Station;
using StationControl.Services.Crm;
using StationControl.Services.Intervention;
using System.Text.Json;

namespace StationControl.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Dashboard")]
    public class DashboardController : Controller
    {
        private readonly IConfiguration _configuration;

        public DashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Dashboard")]
        public IActionResult Dashboard(int? stationId, string statut, int? mois, int? annee)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });

            ViewBag.Utilisateur = utilisateur;

            var stations = StationService.GetAllStationByCrm(connection, utilisateur.CrmId ?? 0);
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

            int m = mois ?? DateTime.Now.Month;
            int y = annee ?? DateTime.Now.Year;

            var interventions = InterventionService.GetInterventions(connection, stationId, statut, m, y);

            ViewBag.Stations = stations;
            ViewBag.CrmChartData = crmChartData;
            ViewBag.Interventions = interventions;
            ViewBag.SelectedStationId = stationId;
            ViewBag.SelectedStatut = statut;
            ViewBag.SelectedMois = m;
            ViewBag.SelectedAnnee = y;

            return View();
        }

        [HttpGet("RealtimeDataAdmin")]
        public IActionResult RealtimeDataAdmin(int? stationId, string statut, int? mois, int? annee)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });

            // ===== Stations =====
            var stations = StationService.GetAllStationByCrm(connection, utilisateur.CrmId ?? 0)
                .Select(s => new
                {
                    s.Id,
                    Nom = s.Nom,
                    Statut = s.Statut,
                    Latitude = s.Latitude?.ToString() ?? "",
                    Longitude = s.Longitude?.ToString() ?? "",
                    TypeStation = s.TypeStation != null ? new { s.TypeStation.Libelle } : null,
                    Region = s.Region != null ? new { s.Region.Libelle } : null,
                    DateDebut = s.DateDebut
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

            int m = mois ?? DateTime.Now.Month;
            int y = annee ?? DateTime.Now.Year;
            var interventions = InterventionService.GetInterventions(connection, stationId, statut, m, y);

            // ===== Ajouter le nom de la station pour chaque intervention =====
            var stationsDict = stations.ToDictionary(s => s.Id, s => s.Nom ?? "-");
            var interventionsWithName = interventions.Select(i => new
            {
                i.Id,
                i.StationId,
                StationName = stationsDict.ContainsKey(i.StationId) ? stationsDict[i.StationId] : "-",
                i.DatePlanifieeDebut,
                i.DateEffectiveDebut,
                i.TechnicienPlanifie,
                i.TechnicienEffectif,
                i.Statut
            }).ToList();

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            };

            return new JsonResult(new
            {
                Stations = stations,
                CrmChartData = crmChartData,
                Interventions = interventionsWithName
            }, jsonOptions);
        }
    }
}
