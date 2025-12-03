using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Services.Auth;
using StationControl.Services.Crm;
using StationControl.Services.Station;

namespace StationControl.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Auth")]
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View("Login");
        }

        [HttpPost("ToLog")]
        public IActionResult ToLog(string email, string motDePasse)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                try
                {
                    Role role = AuthService.GetRoleByLibelle(connection, "Responsable CRM");

                    Utilisateur utilisateur = AuthService.ToLog(connection, email, motDePasse, role);

                    if (utilisateur != null)
                    {
                        var cookieOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Expires = DateTimeOffset.Now.AddHours(4)
                        };

                        Response.Cookies.Append("AdminSession", utilisateur.Id.ToString(), cookieOptions);

                        return RedirectToAction("Dashboard", "Dashboard", new { area = "Admin" });
                    }
                    else
                    {
                        ViewBag.Error = "Email ou mot de passe incorrect.";
                        return View("Login");
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Erreur lors de la connexion : " + ex.Message;
                    return View("Login");
                }
            }
        }

        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            string cookieName = "AdminSession";
            string connection = _configuration.GetConnectionString("DefaultConnection");

            AuthService.Logout(Response, cookieName, connection);

            return RedirectToAction("Login");
        }

        [HttpGet("Inscription")]
        public IActionResult Inscription()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                try
                {
                    var crms = CrmService.GetAllCrms(connection);
                    ViewBag.Error = TempData["Error"];
                    ViewBag.Crms = crms;
                    return View();
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Erreur : " + ex.Message;
                    return View();
                }
            }
        }
        
        [HttpPost("Register")]
        public IActionResult Register(IFormCollection form, IFormFile PhotoProfil)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                try
                {
                    string nom = form["Nom"];
                    string prenom = form["Prenom"];
                    string email = form["Email"];
                    string motDePasse = form["MotDePasse"];
                    string genre = form["Genre"];
                    string crmIdStr = form["CrmId"];

                    if (string.IsNullOrEmpty(nom) || string.IsNullOrEmpty(prenom) ||
                        string.IsNullOrEmpty(email) || string.IsNullOrEmpty(motDePasse) ||
                        string.IsNullOrEmpty(genre) || string.IsNullOrEmpty(crmIdStr))
                    {
                        TempData["ErrorMessage"] = "Tous les champs sont obligatoires.";
                        return RedirectToAction("Inscription");

                    }

                    if (!int.TryParse(crmIdStr, out int crmId))
                    {
                        TempData["ErrorMessage"] = "CRM invalide.";
                        return RedirectToAction("Inscription");
                    }

                    var crm = CrmService.GetCrmById(connection, crmId);
                    if (crm == null)
                    {
                        TempData["ErrorMessage"] = "Le CRM sélectionné n'existe pas.";
                        return RedirectToAction("Inscription");
                    }

                    string photoFileName = null;
                    if (PhotoProfil != null && PhotoProfil.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        photoFileName = Guid.NewGuid() + Path.GetExtension(PhotoProfil.FileName);
                        var filePath = Path.Combine(uploadsFolder, photoFileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        PhotoProfil.CopyTo(stream);
                    }

                    var role = AuthService.GetRoleByLibelle(connection, "Responsable CRM");
                    if (role == null)
                    {
                        TempData["ErrorMessage"] = "Le rôle 'Responsable CRM' n'existe pas.";
                        return RedirectToAction("Inscription");
                    }

                    var utilisateur = new Utilisateur
                    {
                        Nom = nom,
                        Prenom = prenom,
                        Email = email,
                        MotDePasse = motDePasse,
                        Genre = genre,
                        CrmId = crm.Id,
                        Role = role,
                        DateDebut = DateTime.Now,
                        EstValide = false,
                        PhotoProfil = photoFileName
                    };                    
                    TempData["SuccessMessage"] = "Inscription enregistrée. Vous recevrez un email de confirmation après validation.";

                    UtilisateurService.InsertUtilisateur(connection, utilisateur);
                    return RedirectToAction("Inscription");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Erreur lors de l'inscription : " + ex.Message;
                    return RedirectToAction("Inscription");
                }
            }
        }

        [HttpGet("Profile")]
        public IActionResult Profile()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetAdminFromCookie(Request, connection);
                if (utilisateur == null)
                    return RedirectToAction("Login", "Auth", new { area = "Admin" });

            ViewBag.Utilisateur = utilisateur;
            return View("InfosPersonnel", utilisateur);
        }

        [HttpGet("HistoriqueConnexion")]
        public IActionResult HistoriqueConnexion(DateTime? dateDebut, DateTime? dateFin, int page = 1, int pageSize = 10)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });

            var liste = AuthService.GetHistoriqueConnexionByUtilisateur(
                connection,
                dateDebut,
                dateFin,
                utilisateur.Id  
            );

            var totalItems = liste.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var result = liste
                .OrderByDescending(h => h.DateHeureDebut)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Utilisateur = utilisateur;

            ViewData["DateDebut"] = dateDebut?.ToString("yyyy-MM-dd");
            ViewData["DateFin"] = dateFin?.ToString("yyyy-MM-dd");
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;

            var stations = StationService.GetAllStationByCrm(connection, utilisateur.CrmId.Value);

            return View(result);
        }
    }
}
