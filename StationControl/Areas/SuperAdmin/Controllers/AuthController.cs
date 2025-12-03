using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Services.Auth;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Auth")]
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
                    Role role = AuthService.GetRoleByLibelle(connection, "Chef SMIT");
                    Utilisateur utilisateur = AuthService.ToLog(connection, email, motDePasse, role);
                    if (utilisateur != null)
                    {
                        var cookieOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Expires = DateTimeOffset.Now.AddHours(4)
                        };
                        Response.Cookies.Append("SuperAdminSession", utilisateur.Id.ToString(), cookieOptions);
                        return RedirectToAction("Dashboard", "Dashboard");
                    }
                    else
                    {
                        ViewBag.Error = "Email ou mot de passe incorrect.";
                        return View("Login");
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            string cookieName = "SuperAdminSession";
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            AuthService.Logout(Response, cookieName, connectionString);

            return RedirectToAction("Login");
        }

        [HttpGet("Profile")]
        public IActionResult Profile()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
                if (utilisateur == null)
                    return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Utilisateur = utilisateur;
            return View("InfosPersonnel", utilisateur);
        }

        [HttpGet("HistoriqueConnexion")]
        public IActionResult HistoriqueConnexion(DateTime? dateDebut, DateTime? dateFin, int page = 1, int pageSize = 10)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });


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

            return View(result);
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
                    ViewBag.Error = TempData["Error"];
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

                    if (string.IsNullOrEmpty(nom) || string.IsNullOrEmpty(prenom) ||
                        string.IsNullOrEmpty(email) || string.IsNullOrEmpty(motDePasse))
                    {
                        TempData["ErrorMessage"] = "Tous les champs sont obligatoires.";
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

                    var role = AuthService.GetRoleByLibelle(connection, "Chef SMIT");
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


    }
}
