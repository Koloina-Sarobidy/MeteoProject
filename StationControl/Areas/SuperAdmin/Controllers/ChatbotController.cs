using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Services.Auth;
using StationControl.Services.Station;
using System.Text.Json.Serialization;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Chatbot")]
    public class ChatbotController : Controller
    {
        private readonly IConfiguration _configuration;

        public ChatbotController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("HandleQuestion")]
        [IgnoreAntiforgeryToken]
        public IActionResult HandleQuestion([FromBody] ChatbotRequest request)
        {
            string question = request?.Question?.Trim();

            if (string.IsNullOrWhiteSpace(question))
                return Json(new { success = false, message = "Veuillez sélectionner une question." });

            List<Models.Station.Station> stations;

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
                if (utilisateur == null)
                    return Json(new { success = false, message = "Utilisateur non authentifié." });

                stations = question switch
                {
                    "Fonctionnelle" => StationService.GetStationByStatut(connection, "Fonctionnelle"),
                    "Non Fonctionnelle" => StationService.GetStationByStatut(connection, "Non Fonctionnelle"),
                    "Partiellement Fonctionnelle" => StationService.GetStationByStatut(connection, "Partiellement Fonctionnelle"),
                    _ => new List<Models.Station.Station>()
                };
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erreur lors de la récupération des stations : {ex.Message}" });
            }

            if (stations == null || stations.Count == 0)
                return Json(new { success = false, message = "Aucune station trouvée." });

            return Json(new { success = true, question, stations });
        }
    }

    public class ChatbotRequest
    {
        [JsonPropertyName("question")]  
        public string Question { get; set; } = string.Empty;
    }
}
