using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Crm;
using StationControl.Models.Auth;
using StationControl.Models.Equipement;
using System.Collections.Generic;
using StationControl.Services.Auth;
using StationControl.Services.Station;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Equipement")]
    public class EquipementController : Controller
    {
        private readonly IConfiguration _configuration;

        public EquipementController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpPost("InsertCapteur")]
        public IActionResult InsertCapteur(CapteurInsertRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Libelle) || string.IsNullOrWhiteSpace(request.Parametre))
            {
                TempData["ErrorMessage"] = "Les données du Capteur sont invalides.";
                return RedirectToAction("ListeCapteur");
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            try
            {
                var capteur = new Capteur
                {
                    Libelle = request.Libelle.Trim(),
                    Parametre = request.Parametre.Trim()
                };
                CapteurService.InsertCapteur(connection, capteur);

                TempData["SuccessMessage"] = "Capteur créé avec succès !";
                return RedirectToAction("ListeCapteur");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la création du capteur: {ex.Message}";
                return RedirectToAction("ListeCapteur");
            }
        }

        [HttpGet("ListeCapteur")]
        public IActionResult ListeCapteur()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Utilisateur = utilisateur;

            List<Capteur> capteurs = CapteurService.GetAllCapteur(connection);
            ViewBag.Capteurs = capteurs;

            return View("Capteur");
        }

        [HttpPost("InsertAlimentation")]
        public IActionResult InsertAlimentation(AlimentationInsertRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Libelle) || string.IsNullOrWhiteSpace(request.Description))
            {
                TempData["ErrorMessage"] = "Les données de l'Alimentation sont invalides.";
                return RedirectToAction("ListeAlimentation");
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            try
            {
                var alimentation = new Alimentation
                {
                    Libelle = request.Libelle.Trim(),
                    Description = request.Description.Trim()
                };
                AlimentationService.InsertAlimentation(connection, alimentation);

                TempData["SuccessMessage"] = "Alimentation créée avec succès !";
                return RedirectToAction("ListeAlimentation");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la création de l'alimentation: {ex.Message}";
                return RedirectToAction("ListeCapteur");
            }
        }
        [HttpGet("ListeAlimentation")]
        public IActionResult ListeAlimentation()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Utilisateur = utilisateur;

            List<Alimentation> alimentations = AlimentationService.GetAllAlimentation(connection);
            ViewBag.Alimentations = alimentations;

            return View("Alimentation");
        }
    }


    public class CapteurInsertRequest
    {
        public string Libelle { get; set; } = "";
        public string Parametre { get; set; } = "";
    }
    public class AlimentationInsertRequest
    {
        public string Libelle { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
