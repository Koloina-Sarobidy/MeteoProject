using StationControl.Models.Auth;
using System;
using System.Collections.Generic;

namespace StationControl.Models.Intervention
{
    public class PlanifierInterventionViewModel
    {
        public Station.Station Station { get; set; }
        public Utilisateur UtilisateurPlanificateur { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }

        public List<BesoinTechnicien> BesoinsAvecTechnicien { get; set; } = new List<BesoinTechnicien>();
    }

    public class BesoinTechnicien
    {
        public int BesoinId { get; set; }
        public int TechnicienId { get; set; }
    }
}
