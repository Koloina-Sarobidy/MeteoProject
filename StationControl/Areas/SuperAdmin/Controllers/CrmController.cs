using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Crm;
using StationControl.Models.Auth;
using System.Collections.Generic;
using StationControl.Services.Auth;
using StationControl.Services;
using StationControl.Services.Crm;
using System.Reflection.Metadata.Ecma335;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Crm")]
    public class CrmController : Controller
    {
        private readonly IConfiguration _configuration;

        public CrmController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Ajout")]
        public IActionResult Ajout()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Utilisateur = utilisateur;
            return View();
        }

        [HttpPost("Insert")]
        public IActionResult Insert(CrmInsertRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Libelle) || request.DateDebut == default)
            {
                TempData["ErrorMessage"] = "Les données du CRM sont invalides.";
                return RedirectToAction("Ajout"); 
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            try
            {
                var crm = new Crm
                {
                    Libelle = request.Libelle.Trim(),
                    DateDebut = request.DateDebut,
                    DateFin = null,
                    EstArrete = false
                };

                int insert = CrmService.InsertCrm(connection, crm);

                TempData["SuccessMessage"] = "CRM créé avec succès !";
                return RedirectToAction("Liste");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la création du CRM : {ex.Message}";
                return RedirectToAction("Ajout"); 
            }
        }

        [HttpGet("Liste")]
        public IActionResult Liste(string? libelle, DateTime? dateCreation, string? statut)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Utilisateur = utilisateur;

            List<Crm> crms;

            bool? estArrete = null;
            if (!string.IsNullOrEmpty(statut))
                estArrete = statut.ToLower() == "true" || statut.ToLower() == "arrete";

            if (string.IsNullOrEmpty(libelle) && !dateCreation.HasValue && !estArrete.HasValue)
            {
                crms = CrmService.GetAllCrms(connection);
            }
            else
            {
                crms = CrmService.GetCrmsByFilter(connection, libelle, dateCreation, estArrete);
            }

            ViewData["libelle"] = libelle;
            ViewData["dateCreation"] = dateCreation?.ToString("yyyy-MM-dd");
            ViewData["statut"] = statut;

            ViewBag.Crms = crms;

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

            Crm? crm = CrmService.GetCrmById(connection, id);
            if (crm == null)
            {
                TempData["ErrorMessage"] = "Détails CRM introuvables";
                return RedirectToAction("Liste");
            }
            return View(crm);
        }

        [HttpPost("Arreter/{id}")]
        public IActionResult Arreter(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            try
            {
                CrmService.ArreterCrm(connection, id, DateTime.Now);

                TempData["SuccessMessage"] = "CRM arrêté avec succès !";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de l'arrêt du CRM : {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpPost("Modifier/{id}")]
        public IActionResult Modifier(int id, [FromForm] string libelle, [FromForm] List<int>? regions)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            try
            {
                Crm? crm = CrmService.GetCrmById(connection, id);
                if (crm == null)
                    return NotFound();

                crm.Libelle = libelle.Trim();

                CrmService.UpdateCrm(connection, crm);

                TempData["SuccessMessage"] = "CRM mis à jour avec succès !";
                return RedirectToAction("Details", new { id = crm.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la mise à jour : {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }

    }


    public class CrmInsertRequest
    {
        public string Libelle { get; set; } = "";
        public DateTime DateDebut { get; set; }
    }
}
