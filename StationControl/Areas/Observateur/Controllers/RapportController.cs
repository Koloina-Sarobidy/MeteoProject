using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Models.Rapport;
using StationControl.Services.Auth;
using System;
using System.Collections.Generic;
using ClosedXML.Excel;
using StationControl.Services.Station;

namespace StationControl.Areas.Observateur.Controllers
{
    [Area("Observateur")]
    [Route("Observateur/Rapport")]
    public class RapportController : Controller
    {
        private readonly IConfiguration _configuration;

        public RapportController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("RapportMensuelStation")]
        public IActionResult RapportMensuelStation(int? mois, int? annee)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            var now = DateTime.Now;
            int defaultMois = now.Month == 1 ? 12 : now.Month - 1;
            int defaultAnnee = now.Month == 1 ? now.Year - 1 : now.Year;

            int selectedMois = mois ?? defaultMois;
            int selectedAnnee = annee ?? defaultAnnee;

            ViewData["Mois"] = selectedMois;
            ViewData["Annee"] = selectedAnnee;

            int idStation = utilisateur.StationId ?? 0;
            string stationNom = StationService.GetStationById(connection, idStation).Nom; 
            ViewData["StationNom"] = stationNom;

            var service = new RapportService();
            var rapports = service.GetRapportMensuel(connection, selectedMois, selectedAnnee, stationNom);

            ViewBag.Utilisateur = utilisateur;
            return View("Rapport", rapports);
        }

        [HttpPost]
        public IActionResult ExporterExcel(int mois, int annee)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            Utilisateur? utilisateur = AuthService.GetObservateurFromCookie(Request, connection);
            if (utilisateur == null)
                return RedirectToAction("Login", "Auth", new { area = "Observateur" });

            int idStation = utilisateur.StationId ?? 0;
            string stationNom = StationService.GetStationById(connection, idStation).Nom; 
            ViewData["StationNom"] = stationNom;


            var service = new RapportService();
            var rapports = service.GetRapportMensuel(connection, mois, annee, stationNom);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Rapport");

                string[] headers = { "Station", "Date planifiée début", "Date planifiée fin", "Date effective début",
                                    "Date effective fin", "Statut intervention", "Technicien planifié", "Technicien effectif",
                                    "Description besoin", "Équipement", "Numéro série", "Statut équipement" };

                for (int i = 0; i < headers.Length; i++)
                    worksheet.Cell(1, i + 1).Value = headers[i];

                var headerRange = worksheet.Range(1, 1, 1, headers.Length);
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                int row = 2;
                foreach (var r in rapports)
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

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                string fileName = $"Rapport_{mois}_{annee}_{stationNom}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }


    }
}