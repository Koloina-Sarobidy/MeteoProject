using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Models.Station;
using StationControl.Services.Auth;
using System.Collections.Generic;
using System;
using StationControl.Services.Station;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Brand")]
    public class BrandController : Controller
    {
        private readonly IConfiguration _configuration;

        public BrandController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("Insert")]
        public IActionResult Insert(BrandInsertRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Nom))
            {
                TempData["ErrorMessage"] = "Les données du Brand sont invalides.";
                return RedirectToAction("Liste");
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
                if (utilisateur == null)
                    return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });
                
                var brand = new Brand
                {
                    Nom = request.Nom.Trim(),
                };

                BrandService.InsertBrand(connection, brand);

                TempData["SuccessMessage"] = "Brand créé avec succès !";
                return RedirectToAction("Liste");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la création du Brand: {ex.Message}";
                return RedirectToAction("Liste");
            }
        }

        [HttpGet("Liste")]
        public IActionResult Liste()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
                if (utilisateur == null)
                    return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            ViewBag.Utilisateur = utilisateur;

            List<Brand> brands = new List<Brand>();

        
            brands = BrandService.GetAllBrand(connection); 

            ViewBag.Brands = brands;

            return View();
        }
    }

    public class BrandInsertRequest
    {
        public string Nom { get; set; } = "";
    }
}
