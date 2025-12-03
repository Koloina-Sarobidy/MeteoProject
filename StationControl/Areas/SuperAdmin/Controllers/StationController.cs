using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Models.Crm;
using StationControl.Models.Station;
using StationControl.Services.Auth;
using StationControl.Services.Besoin;
using StationControl.Services.Crm;
using StationControl.Services.Station;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Station")]
    public class StationController : Controller
    {
        private readonly IConfiguration _configuration;

        public StationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

         [HttpGet("Ajout")]
        public IActionResult Ajout()
        {
            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Utilisateur = utilisateur;

            ViewBag.Crms = CrmService.GetAllCrms(connection);
            ViewBag.Regions = CrmService.GetAllRegion(connection);
            ViewBag.Brands = BrandService.GetAllBrand(connection);
            ViewBag.TypeStations = StationService.GetAllTypeStation(connection);

            return View();
        }

        [HttpPost("Insert")]
        public IActionResult Insert(StationInsertRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Libelle))
            {
                TempData["ErrorMessage"] = "Les données de la Station sont invalides.";
                return RedirectToAction("Liste");
            }

            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            try
            {
                if (!decimal.TryParse(request.Latitude.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var latitude))
                {
                    TempData["ErrorMessage"] = "Latitude invalide.";
                    return RedirectToAction("Ajout");
                }

                if (!decimal.TryParse(request.Longitude.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var longitude))
                {
                    TempData["ErrorMessage"] = "Longitude invalide.";
                    return RedirectToAction("Ajout");
                }

                var station = new Station
                {
                    Nom = request.Libelle.Trim(),
                    Latitude = latitude,
                    Longitude = longitude,
                    DateDebut = request.DateDebut,
                    Brand = new Brand { Id = request.BrandId },
                    Region = new Region { Id = request.RegionId },
                    TypeStation = new TypeStation { Id = request.TypeStationId },
                    Crm = new Crm { Id = request.CrmId }
                };

                int idStation = StationService.InsertStation(connection, station);
                CrmService.InsertCrmStation(connection, station.Crm.Id, station.Id);
                TempData["SuccessMessage"] = "Station créée avec succès !";
                return RedirectToAction("Liste");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la création de la station : {ex.Message}";
                return RedirectToAction("Liste");
            }
        }

        [HttpGet("Liste")]
        public IActionResult Liste() 
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Utilisateur = utilisateur;

            string nom = Request.Query["Nom"];
            int? brandId = string.IsNullOrEmpty(Request.Query["BrandId"]) ? null : int.Parse(Request.Query["BrandId"]);
            int? regionId = string.IsNullOrEmpty(Request.Query["RegionId"]) ? null : int.Parse(Request.Query["RegionId"]);
            int? crmId = string.IsNullOrEmpty(Request.Query["CrmId"]) ? null : int.Parse(Request.Query["CrmId"]);
            bool? estArrete = string.IsNullOrEmpty(Request.Query["Statut"]) ? null : bool.Parse(Request.Query["Statut"]);
            DateTime? dateDebut = string.IsNullOrEmpty(Request.Query["DateDebut"]) ? null : DateTime.Parse(Request.Query["DateDebut"]);
            DateTime? dateFin = string.IsNullOrEmpty(Request.Query["DateFin"]) ? null : DateTime.Parse(Request.Query["DateFin"]);

            ViewData["Nom"] = nom;
            ViewData["BrandId"] = brandId?.ToString() ?? "";
            ViewData["RegionId"] = regionId?.ToString() ?? "";
            ViewData["CrmId"] = crmId?.ToString() ?? "";
            ViewData["Statut"] = estArrete?.ToString().ToLower() ?? "";
            ViewData["DateDebut"] = dateDebut?.ToString("yyyy-MM-dd") ?? "";
            ViewData["DateFin"] = dateFin?.ToString("yyyy-MM-dd") ?? "";

            var brands = BrandService.GetAllBrand(connection);
            List<Region> regions = CrmService.GetAllRegion(connection);
            var crms = CrmService.GetAllCrms(connection);

            List<Station> stations;
            if (string.IsNullOrEmpty(nom) && brandId == null && regionId == null && crmId == null
                && estArrete == null && dateDebut == null && dateFin == null)
            {
                stations = StationService.GetAllStation(connection);
            }
            else
            {
                stations = StationService.GetStationFiltered(
                    connection,
                    nom,
                    brandId,
                    regionId,
                    crmId,
                    estArrete,
                    dateDebut,
                    dateFin
                );
            }

            ViewBag.Brands = brands;
            ViewBag.Regions = regions;
            ViewBag.Crms = crms;
            ViewBag.Stations = stations;

            return View();
        }


        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Utilisateur = utilisateur;

            Station? station = StationService.GetStationById(connection, id);
            if (station == null)
            {
                TempData["ErrorMessage"] = $"La station avec l'ID {id} n'a pas été trouvée.";
                return RedirectToAction("Liste");
            }

            var capteursStation = StationService.GetCapteurByStation(connection, id) ?? new List<CapteurStation>();
            var alimentationsStation = StationService.GetAlimentationByStation(connection, id) ?? new List<AlimentationStation>();

            var capteurs = CapteurService.GetAllCapteur(connection);
            var alimentations = AlimentationService.GetAllAlimentation(connection);

            ViewBag.Station = station;
            ViewBag.CapteursStation = capteursStation;
            ViewBag.AlimentationsStation = alimentationsStation;
            ViewBag.Capteurs = capteurs;
            ViewBag.Alimentations = alimentations;

            return View();
        }


        [HttpPost("Arret/{id}")]
        public IActionResult Arret(int id, string confirmation)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            Station? station = StationService.GetStationById(connection, id);
            if (station == null)
            {
                TempData["ErrorMessage"] = $"La station avec l'ID {id} n'a pas été trouvée.";
                return RedirectToAction("Details", new { id });
            }

            if (confirmation != "1")
            {
                TempData["ErrorMessage"] = "Vous devez taper 1 pour confirmer l'arrêt !";
                return RedirectToAction("Details", new { id });
            }

            try
            {
                StationService.ArretStation(connection, id);
                TempData["SuccessMessage"] = "Station arrêtée avec succès";
                return RedirectToAction("Details", new { id });

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de l'arrêt de la station : {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpPost("AjouterCapteur")]
        public IActionResult AjouterCapteur(CapteurStationInsertRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NumSerie))
            {
                TempData["ErrorMessage"] = "Les données du capteur sont invalides.";
                return RedirectToAction("Details", new { id = request?.StationId ?? 0 });
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            try
            {
                var capteurStation = new CapteurStation
                {
                    Station = new Station { Id = request.StationId },
                    Capteur = new Models.Equipement.Capteur { Id = request.CapteurId },
                    DateDebut = request.DateDebut,
                    DateFin = request.DateFin,
                    NumSerie = request.NumSerie,
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

            return RedirectToAction("Details", new { id = request.StationId });
        }


        [HttpPost("AjouterAlimentation")]
        public IActionResult AjouterAlimentation(AlimentationStationInsertRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NumSerie))
            {
                TempData["ErrorMessage"] = "Les données de l'alimentation sont invalides.";
                return RedirectToAction("Details", new { id = request?.StationId ?? 0 });
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

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

            return RedirectToAction("Details", new { id = request.StationId });
        }


    }
    public class StationInsertRequest
    {
        public string Libelle { get; set; } = "";
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public DateTime DateDebut { get; set; }
        public int BrandId { get; set; }
        public int RegionId { get; set; }
        public int TypeStationId { get; set; }
        public int CrmId { get; set; }
    }
    public class CapteurStationInsertRequest
    {
        public int StationId { get; set; }
        public int CapteurId { get; set; }
        public string NumSerie { get; set; }
        public DateTime DateDebut { get; set; } = DateTime.Now;
        public DateTime? DateFin { get; set; }
        public string? Statut { get; set; }
    }
    public class AlimentationStationInsertRequest
    {
        public int StationId { get; set; }
        public int AlimentationId { get; set; }
        public string NumSerie { get; set; }
        public DateTime DateDebut { get; set; } = DateTime.Now;
        public DateTime? DateFin { get; set; }
        public string? Statut { get; set; }
    }
}
