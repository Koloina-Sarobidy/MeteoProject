using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Models.Intervention;
using StationControl.Services.Auth;
using StationControl.Services.Intervention;
using StationControl.Services.Inventaire;
using StationControl.Services.Station;


namespace StationControl.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Intervention")]
    public class InterventionController : Controller
    {
        private readonly IConfiguration _configuration;

        public InterventionController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("PlanifierBesoins")]
        public IActionResult PlanifierBesoins(int stationId,
                                                [FromForm] DateTime dateDebut,
                                                [FromForm] DateTime? dateFin,
                                                [FromForm] List<int> BesoinIds,
                                                [FromForm] List<int> TechnicienIds)
        {
                if (BesoinIds == null || BesoinIds.Count == 0)
                {
                    TempData["Message"] = "Aucun besoin sélectionné.";
                    return RedirectToAction("Details", new { stationId });
                }

                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                Utilisateur? utilisateurPlanificateur = AuthService.GetAdminFromCookie(Request, connection);
                if (utilisateurPlanificateur == null)
                {
                    TempData["Message"] = "Utilisateur non authentifié.";
                    return RedirectToAction("Details", new { stationId });
                }

                var station = StationService.GetStationById(connection, stationId);
                if (station == null)
                {
                    TempData["Message"] = "Station introuvable.";
                    return RedirectToAction("Details", new { stationId });
                }

                var besoinsAvecTechnicien = new List<BesoinTechnicien>();
                for (int i = 0; i < BesoinIds.Count; i++)
                {
                    int technicienId = (TechnicienIds != null && TechnicienIds.Count > i) ? TechnicienIds[i] : 0;
                    besoinsAvecTechnicien.Add(new BesoinTechnicien
                    {
                        BesoinId = BesoinIds[i],
                        TechnicienId = technicienId
                    });
                }

                var model = new PlanifierInterventionViewModel
                {
                    Station = station,
                    UtilisateurPlanificateur = utilisateurPlanificateur,
                    DateDebut = dateDebut,
                    DateFin = dateFin,
                    BesoinsAvecTechnicien = besoinsAvecTechnicien
                };

                TempData["Message"] = $"{BesoinIds.Count} besoin(s) planifié(s) pour la période du {dateDebut:dd/MM/yyyy}" +
                                    (dateFin.HasValue ? $" au {dateFin:dd/MM/yyyy}" : ".");

                return RedirectToAction("Details", new { stationId });
            }
    }
}
