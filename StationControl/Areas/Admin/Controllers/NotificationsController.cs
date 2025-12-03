using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Services.Auth;
using StationControl.Services.Station;
using StationControl.Services.Besoin;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using StationControl.Models.Auth;

namespace StationControl.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Notifications")]
    public class NotificationController : Controller
    {
        private readonly IConfiguration _configuration;

        public NotificationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("BesoinsNonTraites")]
        public IActionResult BesoinsNonTraites()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                Utilisateur? utilisateur = AuthService.GetAdminFromCookie(Request, connection);
                if (utilisateur == null)
                    return RedirectToAction("Login", "Auth", new { area = "Admin" });

                int crmId = utilisateur.CrmId.Value;

                var besoins = BesoinService.GetBesoinsNonTraitesNonRenvoyesParCrm(connection, crmId);

                var notifications = new List<object>();

                foreach (var b in besoins)
                {
                    notifications.Add(new
                    {
                        StationNom = b.NomStation,
                        Message = $"Le besoin #{b.BesoinId} n'a pas encore été traité pour la station {b.NomStation}."
                    });
                }

                return Json(notifications);
            }
            catch (Exception ex)
            {
                return Json(new[] { new { Message = "Erreur: " + ex.Message } });
            }
        }
    }
}
