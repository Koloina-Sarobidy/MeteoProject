using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Services.Auth;
using MailKit.Net.Smtp;
using MimeKit;
using System.Security.Cryptography;
using System.ComponentModel;


namespace StationControl.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;
        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public IActionResult RedirectByRole(string role)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                switch (role.ToLower())
                {
                    case "chef-smit":
                        {
                            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
                            if (utilisateur != null)
                            {
                                return RedirectToAction("Dashboard", "Dashboard", new { area = "SuperAdmin" });
                            }
                            return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });
                        }

                    case "responsable-crm":
                        {
                            Utilisateur? utilisateur = AuthService.GetAdminFromCookie(Request, connection);
                            if (utilisateur != null)
                            {
                                return RedirectToAction("Dashboard", "Dashboard", new { area = "Admin" });
                            }
                            return RedirectToAction("Login", "Auth", new { area = "Admin" });
                        }

                    case "observateur":
                        {
                            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
                            if (utilisateur != null)
                            {
                                return RedirectToAction("Dashboard", "Dashboard", new { area = "Observateur" });
                            }
                            return RedirectToAction("Login", "Auth", new { area = "Observateur" });
                        }

                    default:
                        return RedirectToAction("Index", "Home");
                }
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword(string roleString)
        {
            TempData["role"] = roleString;
            return View(); 
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email, string roleString)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            if (roleString == "SuperAdmin")
                roleString = "Chef SMIT";
            else if (roleString == "Crm")
                roleString = "Responsable CRM";
            else if (roleString == "Observateur")
                roleString = "Observateur";

            Role role = AuthService.GetRoleByLibelle(connection, roleString);

            var utilisateur = UtilisateurService.GetUserByEmail(email, role, connection); 
            if (utilisateur == null)
            {
                TempData["ErrorMessage"] = "Email non trouvé";
                return RedirectToAction("ForgotPassword", new { roleString = roleString});
            }

            string code = GenerateurCode.GenerateResetCode();
            GenerateurCode.SaveResetCode(connection, utilisateur.Id, code);

            var emailService = new EmailService(_configuration);
            await emailService.SendResetCode(email, code);
            
            return RedirectToAction("EnterResetCode", new { userId = utilisateur.Id });
        }

        
        [HttpGet]
        public IActionResult EnterResetCode(int userId)
        {
            TempData["SuccessMessage"] = "Un code vous a été envoyé par email.";
            ViewBag.UserId = userId;
            return View();
        }

        [HttpPost]
        public IActionResult VerifyResetCode(int userId, string code)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            bool valid = GenerateurCode.ValidateCode(connection, userId, code);
            if (!valid)
            {
                TempData["ErrorMessage"] = "Code invalide ou expiré.";
                return RedirectToAction("EnterResetCode", new { userId });
            }

            return RedirectToAction("ResetPassword", new { userId });
        }

        [HttpGet]
        public IActionResult ResetPassword(int userId)
        {
            ViewBag.UserId = userId;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(int userId, string newPassword)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connStr);
            connection.Open();

            UtilisateurService.UpdateMotDePasse(connection, userId, newPassword); 

            TempData["SuccessMessage"] = "Mot de passe mis à jour.";
            return RedirectToAction("Index", "Home"); 
        }
    }
}
