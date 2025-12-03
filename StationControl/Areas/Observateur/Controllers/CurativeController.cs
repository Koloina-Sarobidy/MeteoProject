using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Models.Besoin;
using StationControl.Models.Station;
using StationControl.Services.Auth;
using StationControl.Services.Besoin;
using StationControl.Services.Intervention;
using StationControl.Services.Station;

namespace StationControl.Areas.Observateur.Controllers
{
    [Area("Observateur")]
    [Route("Observateur/Curative")]
    public class CurativeController : Controller
    {
        private readonly IConfiguration _configuration;

        public CurativeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Liste")]
        public IActionResult Liste()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var observateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (observateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            int stationId = observateur.StationId ?? 0;
            List<BesoinStation> besoins;
            try
            {
                besoins = BesoinService.GetBesoinTraiteMaisNonCompleteByStation(connection, stationId);
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la récupération des besoins et interventions : {ex.Message}";
                besoins = new List<BesoinStation>();
            }
            ViewBag.Utilisateur = observateur;
            return View(besoins);
        }

        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            int stationId = utilisateur.StationId ?? 0;
            var besoins = BesoinService.GetBesoinTraiteMaisNonCompleteByStation(connection, stationId);
            var besoin = besoins.FirstOrDefault(b => b.Id == id);


            if (besoin == null)
            {
                TempData["ErrorMessage"] = "Besoin introuvable.";
                return RedirectToAction("Liste");
            }
            int idStation = utilisateur.StationId ?? 0;
            var intervention = InterventionService.GetInterventionByBesoinId(connection, id);

            ViewBag.Utilisateur = utilisateur;
            ViewBag.Intervention = intervention;

            return View(besoin);
        }

        [HttpGet("DetailBesoin/{idEquipement}")]
        public IActionResult DetailBesoin(int idEquipement)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            int idStation = utilisateur.StationId ?? 0;
            var besoin = BesoinService.GetBesoinByEquipementStation(connection, idStation, idEquipement);
            int idBesoin = besoin.Id;

            ViewBag.Utilisateur = utilisateur;
            var intervention = InterventionService.GetInterventionByBesoinId(connection, idBesoin);

            ViewBag.Intervention = intervention;

            return View("Details", besoin);
        }

        [HttpPost("Effectuer/{id}")]
        public IActionResult Effectuer(int id, DateTime dateDebut, DateTime? dateFin, string executeur, int besoinId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                Utilisateur utilisateur;
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    utilisateur = AuthService.GetObservateurFromCookie(Request, conn);
                }

                if (utilisateur == null)
                    return RedirectToAction("Login", "Auth", new { area = "Observateur" });

                int stationId = utilisateur.StationId ?? 0;
                string? nouveauNumSerie = Request.Form["nouveauNumSerie"];

                // Si un nouveau numéro de série est fourni
                if (!string.IsNullOrWhiteSpace(nouveauNumSerie))
                {
                    BesoinStation? besoin;
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        besoin = BesoinService.GetBesoinById(conn, besoinId);
                    }

                    if (besoin != null)
                    {
                        if (besoin.CapteurStation != null)
                        {
                            var capteurStation = new CapteurStation
                            {
                                Station = new Station { Id = stationId },
                                Capteur = new StationControl.Models.Equipement.Capteur
                                {
                                    Id = besoin.CapteurStation.Capteur.Id
                                },
                                NumSerie = nouveauNumSerie.Trim(),
                                DateDebut = DateTime.Now,
                                DateFin = null,
                                EstimationVieAnnee = 3,
                                Statut = "Fonctionnel"
                            };

                            using (var conn = new MySqlConnection(connectionString))
                            {
                                conn.Open();
                                StationService.InsertCapteurStation(conn, capteurStation);
                            }
                        }
                        else if (besoin.AlimentationStation != null)
                        {
                            var alimStation = new AlimentationStation
                            {
                                Station = new Station { Id = stationId },
                                Alimentation = new StationControl.Models.Equipement.Alimentation
                                {
                                    Id = besoin.AlimentationStation.Alimentation.Id
                                },
                                NumSerie = nouveauNumSerie.Trim(),
                                DateDebut = DateTime.Now,
                                DateFin = null,
                                EstimationVieAnnee = 3,
                                Statut = "Fonctionnel"
                            };

                            using (var conn = new MySqlConnection(connectionString))
                            {
                                conn.Open();
                                StationService.InsertAlimentationStation(conn, alimStation);
                            }
                        }
                    }
                }

                // Effectuer l'intervention
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    InterventionService.EffectuerIntervention(
                        conn,
                        id,
                        dateDebut,
                        dateFin,
                        executeur,
                        utilisateur.Id
                    );
                }

                // Compléter le besoin si dateFin est fournie
                if (dateFin != null)
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        BesoinService.CompleterBesoin(conn, besoinId);
                    }
                }

                TempData["SuccessMessage"] = "Intervention effectuée avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de l'intervention : {ex.Message}";
            }

            return RedirectToAction("Liste");
        }


        [HttpPost("Terminer/{id}")]
        public IActionResult Terminer(int id, DateTime dateFin, int besoinId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            try
            {
                BesoinService.CompleterBesoin(connection, besoinId);
                InterventionService.TerminerIntervention(connection, id, dateFin);
                TempData["SuccessMessage"] = "Intervention terminée avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la finalisation de l'intervention : {ex.Message}";
            }

            return RedirectToAction("Liste");
        }


    }
}
