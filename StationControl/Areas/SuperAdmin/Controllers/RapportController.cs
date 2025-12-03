using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Models.Rapport;
using StationControl.Services.Auth;
using System;
using System.Collections.Generic;
using ClosedXML.Excel;

namespace StationControl.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Route("SuperAdmin/Rapport")]
    public class RapportController : Controller
    {
        private readonly IConfiguration _configuration;

        public RapportController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("RapportMensuelStation")]
        public IActionResult RapportMensuelStation(string stationNom, int? mois, int? annee)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });
                
            var now = DateTime.Now;
            int defaultMois = now.Month == 1 ? 12 : now.Month - 1;
            int defaultAnnee = now.Month == 1 ? now.Year - 1 : now.Year;

            int selectedMois = mois ?? defaultMois;
            int selectedAnnee = annee ?? defaultAnnee;

            ViewData["Mois"] = selectedMois;
            ViewData["Annee"] = selectedAnnee;
            ViewData["StationNom"] = stationNom ?? string.Empty;

            ViewBag.Utilisateur = utilisateur;

            var rapports = new List<RapportMensuelStation>();
        
            var service = new RapportService();
            rapports = service.GetRapportMensuel(connection, selectedMois, selectedAnnee, stationNom);

            return View("Rapport", rapports);
        }


       [HttpPost]
        public IActionResult ExporterExcel(int mois, int annee, string stationNom)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetSuperAdminFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "SuperAdmin" });

            var service = new RapportService();
            var rapports = service.GetRapportMensuel(connection, mois, annee, stationNom);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Rapport");

                worksheet.Cell(1, 1).Value = "Station";
                worksheet.Cell(1, 2).Value = "Date planifiée début";
                worksheet.Cell(1, 3).Value = "Date planifiée fin";
                worksheet.Cell(1, 4).Value = "Date effective début";
                worksheet.Cell(1, 5).Value = "Date effective fin";
                worksheet.Cell(1, 6).Value = "Statut intervention";
                worksheet.Cell(1, 7).Value = "Technicien planifié";
                worksheet.Cell(1, 8).Value = "Technicien effectif";
                worksheet.Cell(1, 9).Value = "Description besoin";
                worksheet.Cell(1, 10).Value = "Équipement";
                worksheet.Cell(1, 11).Value = "Numéro série";
                worksheet.Cell(1, 12).Value = "Statut équipement";

                var headerRange = worksheet.Range("A1:L1");
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen; 
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                int row = 2;
                foreach(var r in rapports)
                {
                    worksheet.Cell(row, 1).Value = r.StationNom;
                    worksheet.Cell(row, 2).Value = r.DatePlanifieeDebut?.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 3).Value = r.DatePlanifieeFin?.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 4).Value = r.DateEffectiveDebut?.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 5).Value = r.DateEffectiveFin?.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 6).Value = r.StatutIntervention;
                    worksheet.Cell(row, 7).Value = r.TechnicienPlanifie;
                    worksheet.Cell(row, 8).Value = r.TechnicienEffectif;
                    worksheet.Cell(row, 9).Value = r.BesoinDescription;
                    worksheet.Cell(row, 10).Value = r.EquipementLibelle;
                    worksheet.Cell(row, 11).Value = r.EquipementNumSerie;
                    worksheet.Cell(row, 12).Value = r.EquipementStatut;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    string fileName = $"Rapport_{mois}_{annee}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

    }
}