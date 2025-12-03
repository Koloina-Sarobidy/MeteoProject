using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Services.Auth;
using StationControl.Services.Besoin;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Notifications")]
    public class NotificationController : Controller
    {
        private readonly IConfiguration _config;

        public NotificationController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("BesoinsRenvoyes")]
        public IActionResult BesoinsRenvoyes()
        {
            try
            {
                string connString = _config.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connString);
                connection.Open();

                // Vérification login
                var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
                if (utilisateur == null)
                    return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

                // Récupération sans filtrage
                var besoins = BesoinService.GetBesoinsRenvoyesPourChefSmit(connection);

                var notifications = new List<object>();

                foreach (var b in besoins)
                {
                    notifications.Add(new
                    {
                        StationNom = b.NomStation,
                        Message = $"Le besoin #{b.BesoinId} a été renvoyé pour la station {b.NomStation}."
                    });
                }

                return Json(notifications);
            }
            catch (Exception ex)
            {
                return Json(new[]
                {
                    new { Message = "Erreur: " + ex.Message }
                });
            }
        }
    }
}
