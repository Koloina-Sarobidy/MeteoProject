using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Services.Auth;
using Microsoft.Extensions.Configuration;
using System.Data;
using ClosedXML.Excel;
using System.IO;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Dashboard")]
    public class ExportController : Controller
    {
        private readonly IConfiguration _config;

        public ExportController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("ExportExcel")]
        public IActionResult ExportExcel()
        {
            try
            {
                string connectionString = _config.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                var utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
                if (utilisateur == null)
                    return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

                return ExportStationsToExcel(connection);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erreur lors de l'export : " + ex.Message;
                return RedirectToAction("Dashboard", "Dashboard"); 
            }
        }

        private IActionResult ExportStationsToExcel(MySqlConnection connection)
        {
            var dt = new DataTable();

            string query = "SELECT * FROM vue_stations_complet ORDER BY crm, region, city_name, equipement_station_id";
            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();
            dt.Load(reader);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Stations");

            for (int col = 0; col < dt.Columns.Count; col++)
            {
                var headerCell = ws.Cell(1, col + 1);
                headerCell.Value = dt.Columns[col].ColumnName;
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Fill.BackgroundColor = XLColor.Green; 
                headerCell.Style.Font.FontColor = XLColor.White; 
            }

            int currentRow = 2;
            string previousStation = null;

            for (int row = 0; row < dt.Rows.Count; row++)
            {
                string currentStation = dt.Rows[row]["city_name"]?.ToString();

                if (previousStation != null && currentStation != previousStation)
                {
                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        ws.Cell(currentRow, col + 1).Style.Fill.BackgroundColor = XLColor.Yellow;
                    }
                    currentRow++;
                }

                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    var cell = ws.Cell(currentRow, col + 1);
                    var value = dt.Rows[row][col];

                    if (value == DBNull.Value)
                    {
                        cell.Value = "";
                    }
                    else if (value is DateTime dtValue)
                    {
                        cell.Value = dtValue;
                        cell.Style.DateFormat.Format = "dd/MM/yyyy";
                    }
                    else if (value is int || value is long || value is decimal || value is double || value is float)
                    {
                        cell.Value = Convert.ToDouble(value);
                    }
                    else
                    {
                        cell.Value = value.ToString();
                    }

                    if (dt.Columns[col].ColumnName == "equipement_statut")
                    {
                        string statut = value?.ToString()?.ToLower() ?? "";
                        if (statut == "fonctionnel" || statut == "f")
                            cell.Style.Font.FontColor = XLColor.Green;
                        else if (statut == "partiellement fonctionnelle" || statut == "pf")
                            cell.Style.Font.FontColor = XLColor.Orange;
                        else if (statut == "non fonctionnelle" || statut == "nf")
                            cell.Style.Font.FontColor = XLColor.Red;
                    }
                }

                previousStation = currentStation;
                currentRow++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"Stations_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
        }


    }
}
