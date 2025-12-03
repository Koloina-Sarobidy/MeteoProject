using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Services.Inventaire;
using StationControl.Services.Auth;
using StationControl.Models.Auth;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using StationControl.Services.Station;
using StationControl.Services.Intervention;

namespace StationControl.Areas.Observateur.Controllers
{
    [Area("Observateur")]
    [Route("Observateur/Notifications")]
    public class NotificationController : Controller
    {
        private readonly IConfiguration _configuration;

        public NotificationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("InventaireNotifications")]
        public IActionResult InventaireNotifications()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                var utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
                if (utilisateur == null || utilisateur.StationId == null)
                    return Json(new List<object>());

                var station = StationService.GetStationById(connection, utilisateur.StationId.Value);
                if (station == null)
                    return Json(new List<object>());

                var notifications = new List<object>();

                if (InventaireService.EstInventaireNecessaire(connection, utilisateur.StationId.Value))
                {
                    notifications.Add(new
                    {
                        StationNom = station.Nom,
                        Message = $"L'inventaire doit être effectué pour la station {station.Nom}."
                    });
                }

                if (PreventiveService.EstPreventiveEnRetard(connection, utilisateur.StationId.Value))
                {
                    notifications.Add(new
                    {
                        StationNom = station.Nom,
                        Message = $"Il y a des actions préventives en retard pour la station {station.Nom}."
                    });
                }

                if (InterventionService.EstInterventionEnRetard(connection, utilisateur.StationId.Value))
                {
                    notifications.Add(new
                    {
                        StationNom = station.Nom,
                        Message = $"Il y a des interventions en retard pour la station {station.Nom}."
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
