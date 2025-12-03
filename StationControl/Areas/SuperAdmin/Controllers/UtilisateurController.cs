using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Services.Auth;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Utilisateur")]
    public class UtilisateurController : Controller
    {
        private readonly IConfiguration _configuration;

        public UtilisateurController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("EditProfile")]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(Utilisateur formUtilisateur, IFormFile PhotoProfil)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateurEnCours = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateurEnCours == null)
                return RedirectToAction("Login", "Auth");

            utilisateurEnCours.Nom = formUtilisateur.Nom;
            utilisateurEnCours.Prenom = formUtilisateur.Prenom;
            utilisateurEnCours.Email = formUtilisateur.Email;
            utilisateurEnCours.DateDebut = formUtilisateur.DateDebut;
            utilisateurEnCours.DateFin = formUtilisateur.DateFin;

            if (PhotoProfil != null && PhotoProfil.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string photoFileName = Guid.NewGuid() + Path.GetExtension(PhotoProfil.FileName);
                var filePath = Path.Combine(uploadsFolder, photoFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    PhotoProfil.CopyTo(stream);
                }

                utilisateurEnCours.PhotoProfil = photoFileName;
            }

            UtilisateurService.UpdateUtilisateur(connection, utilisateurEnCours);

            TempData["SuccessMessage"] = "Profil mis à jour avec succès.";
            return RedirectToAction("Profile", "Auth");
        }

        [HttpPost("ChangePassword")]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateurEnCours = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateurEnCours == null)
                return RedirectToAction("Login", "Auth");

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var currentHashed = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(CurrentPassword)));

            if (utilisateurEnCours.MotDePasse != currentHashed)
            {
                TempData["ErrorMessage"] = "Le mot de passe actuel est incorrect.";
                return RedirectToAction("Profile", "Auth");
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Le nouveau mot de passe et sa confirmation ne correspondent pas.";
                return RedirectToAction("Profile", "Auth");
            }

            if (NewPassword.Length < 5)
            {
                TempData["ErrorMessage"] = "Le mot de passe doit contenir au moins 5 caractères.";
                return RedirectToAction("Profile", "Auth");
            }

            UtilisateurService.UpdateMotDePasse(connection, utilisateurEnCours.Id, NewPassword);

            TempData["SuccessMessage"] = "Mot de passe modifié avec succès.";
            return RedirectToAction("Profile", "Auth");
        }

        [HttpGet("RedirectStatut")]
        public IActionResult RedirectStatut()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var superAdmin = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (superAdmin == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            int idSuperAdmin = superAdmin.Id;
            var statut = UtilisateurStatutHistoriqueService.GetLastStatutByUtilisateur(connection, idSuperAdmin);

            ViewBag.Utilisateur = superAdmin;
            return View("Statut");
        }

        [HttpPost("ModifierStatut")]
        public IActionResult ModifierStatut(string statut, string? description, DateTime dateDebut, DateTime? dateFin)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var superAdmin = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (superAdmin == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            UtilisateurService.ModifierStatut(connection, superAdmin.Id, statut, superAdmin.Id, description, dateDebut, dateFin);

            TempData["SuccessMessage"] = "Statut mis à jour avec succès.";

            return RedirectToAction("RedirectStatut");
        }

    }
}
