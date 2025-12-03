using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models;
using StationControl.Models.Auth;
using StationControl.Services.Auth;

namespace StationControl.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        string connectionString = _configuration.GetConnectionString("DefaultConnection");
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            try
            {
                List<Role> roles = AuthService.GetAllRole(connection);
                ViewBag.Roles = roles;
            }
            catch(Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur : {ex.Message}";
            }
            finally
            {
                connection.Close();
            }
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
