using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Models.Station;
using StationControl.Services.Auth;
using StationControl.Services.Crm;
using StationControl.Services.Station;

namespace StationControl.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Station")]
    public class StationController : Controller
    {
        private readonly IConfiguration _configuration;

        public StationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Admin" });

            ViewBag.Utilisateur = utilisateur;

            Station? station = StationService.GetStationById(connection, id);
            if (station == null)
                return NotFound($"La station avec l'ID {id} n'a pas été trouvée.");

            var alimentationsStation = StationService.GetAlimentationByStation(connection, id);
            var capteursStation = StationService.GetCapteurByStation(connection, id);

            ViewBag.AlimentationsStation = alimentationsStation;
            ViewBag.CapteursStation = capteursStation;
            ViewBag.Station = station;

            int crmId = ViewBag.Utilisateur.CrmId;

            List<Station> stations = StationService.GetAllStationByCrm(connection, crmId);

            ViewBag.Stations = stations;
            return View();
        }
    }
}
