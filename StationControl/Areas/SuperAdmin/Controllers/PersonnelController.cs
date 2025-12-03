using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Models.Crm;
using StationControl.Models.Personnel;
using StationControl.Services.Auth;
using StationControl.Services.Crm;
using StationControl.Services.Station;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Personnel")]
    public class PersonnelController : Controller
    {
        private readonly IConfiguration _configuration;

        public PersonnelController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Inscription")]
        public IActionResult Inscription()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            var model = new DemandesInscriptionViewModel
            {
                DemandesSuperAdmin = UtilisateurService.GetSuperAdminNonValides(connection),
                DemandesCRM = UtilisateurService.GetResponsablesCRMNonValides(connection),
                DemandesObservateurs = UtilisateurService.GetObservateursNonValides(connection),
                Crms = CrmService.GetAllCrms(connection).ToDictionary(c => c.Id, c => c.Libelle),
                Stations = StationService.GetAllStation(connection).ToDictionary(s => s.Id, s => s.Nom)
            };

            ViewBag.Utilisateur = utilisateur;

            return View(model);
        }

        [HttpPost("Valider")]
        public IActionResult Valider(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            try
            {
                UtilisateurService.ValiderUtilisateur(connection, id);
                TempData["SuccessMessage"] = "Utilisateur validé avec succès !";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la validation de l'utilisateur: {ex.Message}";
            }

            var utilisateurValid = UtilisateurService.GetUtilisateurById(connection, id);
            EmailService.SendEmail(_configuration, utilisateurValid, "Validation Inscription", "Votre demande d'inscription dans StationControl est validée.");
            return RedirectToAction("Inscription");
        }

        [HttpPost("Supprimer")]
        public IActionResult Supprimer(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? superAdmin = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (superAdmin == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            try
            {
                bool success = UtilisateurService.DeleteUtilisateur(connection, id);

                if (success)
                    TempData["SuccessMessage"] = "Utilisateur supprimé avec succès.";
                else
                    TempData["ErrorMessage"] = "Impossible de supprimer l'utilisateur : utilisateur introuvable.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la suppression : {ex.Message}";
            }
            return RedirectToAction("Inscription");
        }


        [HttpGet("ListeResponsablesCrm")]
        public IActionResult ListeResponsablesCrm(
            int? crmId,
            DateTime? dateDebut,
            string statut,
            int page = 1,
            int pageSize = 10)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            var crms = CrmService.GetAllCrms(connection);

            var listeComplete = UtilisateurService.GetResponsablesCrmFiltres(connection, crmId, dateDebut, statut);

            var totalCount = listeComplete.Count;
            var listePage = listeComplete
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            var vm = new ListeResponsablesCrmViewModel
            {
                Responsables = listePage,
                Crms = crms,
                CrmId = crmId,
                DateDebut = dateDebut,
                Statut = statut,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View("Crms", vm);
        }

        [HttpGet("ListeObservateurs")]
        public IActionResult ListeObservateurs(int? stationId, DateTime? dateDebut, string statut)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            var stations = StationService.GetAllStation(connection);
            ViewBag.Stations = stations;
            ViewBag.FiltreStationId = stationId;
            ViewBag.FiltreDateDebut = dateDebut;
            ViewBag.FiltreStatut = statut;

            var observateurs = UtilisateurService.GetObservateursFiltres(connection, stationId, dateDebut, statut);

            var observateursParStation = stations.ToDictionary(
                s => s,
                s => observateurs.Where(u => u.StationId == s.Id).ToList()
            );

            return View("Observateurs", observateursParStation);
        }

        [HttpGet("DetailsResponsableCrm/{id}")]
        public IActionResult DetailsResponsableCrm(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? superAdmin = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (superAdmin == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            Utilisateur responsable = UtilisateurService.GetUtilisateurById(connection, id);
            if (responsable == null)
            {
                TempData["ErrorMessage"] = "Détails Responsable CRM introuvable";
                return RedirectToAction("ListeResponsablesCrm");
            }

            int idResponsable = responsable.Id;
            var statut = UtilisateurStatutHistoriqueService.GetLastStatutByUtilisateur(connection, idResponsable);

            int idCrm = responsable.CrmId ?? 0;
            var crm = CrmService.GetCrmById(connection, idCrm);
            ViewBag.Crm = crm;

            ViewBag.Statut = statut;
            ViewBag.Utilisateur = superAdmin;
            return View("DetailsResponsableCrm", responsable);
        }

        [HttpGet("DetailsObservateur/{id}")]
        public IActionResult DetailsObservateur(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? superAdmin = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (superAdmin == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            Utilisateur observateur = UtilisateurService.GetUtilisateurById(connection, id);
            if (observateur == null)
            {
                TempData["ErrorMessage"] = "Détails Observateur introuvable";
                return RedirectToAction("ListeObservateurs");
            }

            int idObservateur = observateur.Id;
            var statut = UtilisateurStatutHistoriqueService.GetLastStatutByUtilisateur(connection, idObservateur);

            int idStation = observateur.StationId ?? 0;
            var station = StationService.GetStationById(connection, idStation);
            ViewBag.Station = station;

            ViewBag.Statut = statut;
            ViewBag.Utilisateur = superAdmin;
            return View("DetailsObservateur", observateur);
        }
    }
}
