using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Models.Intervention;
using StationControl.Models.Station;
using StationControl.Services.Besoin;
using StationControl.Services.Auth;
using StationControl.Services.Intervention;
using StationControl.Services.Station;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Intervention")]
    public class InterventionController : Controller
    {
        private readonly IConfiguration _configuration;

        public InterventionController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet("Preventive")]
        public IActionResult Preventive(
            int page = 1,
            string? StationNom = null,
            DateTime? DatePrevueDebut = null,
            DateTime? DateEffectiveDebut = null,
            int? PlanificateurId = null,
            int? EffectifId = null)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            var planificateurs = UtilisateurService.GetUtilisateurByRole(connection, "Observateur");
            var effectifs = UtilisateurService.GetUtilisateurByRole(connection, "Observateur");

            var interventions = PreventiveService.GetPreventiveByFilter(
                connection,
                stationNom: StationNom,
                datePrevueDebut: DatePrevueDebut,
                dateEffectiveDebut: DateEffectiveDebut,
                planificateurId: PlanificateurId,
                effectifId: EffectifId
            );

            int pageSize = 10;
            int totalPages = (int)Math.Ceiling(interventions.Count / (double)pageSize);
            var pagedInterventions = interventions
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToList();

            var viewModel = new PreventiveViewModel
            {
                Interventions = pagedInterventions,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                Filtre = new PreventiveFiltre
                {
                    StationNom = StationNom,
                    DatePrevueDebut = DatePrevueDebut,
                    DateEffectiveDebut = DateEffectiveDebut,
                    PlanificateurId = PlanificateurId,
                    EffectifId = EffectifId
                },
                Planificateurs = planificateurs,
                Effectifs = effectifs
            };

            return View(viewModel);
        }

        [HttpGet("Curative")]
        public IActionResult Curative(int? stationId = null, string statut = null, int? mois = null, int? annee = null)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            var interventions = InterventionService.GetInterventions(
                connection,
                stationId,
                statut,
                mois,
                annee
            );

            var stations = StationService.GetAllStation(connection);

            DateTime now = DateTime.Now;

            ViewBag.Mois = mois ?? now.Month;
            ViewBag.Annee = annee ?? now.Year;
            ViewBag.StationId = stationId;
            ViewBag.Statut = statut;
            ViewBag.Interventions = interventions;
            ViewBag.Stations = stations;
            ViewBag.Utilisateur = utilisateur;

            return View();
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

    }
}
