using System;

namespace StationControl.Models.Intervention
{
    public class Intervention
    {
        public int Id { get; set; }

        public int StationId { get; set; }

        public DateTime? Date { get; set; }

        public int? BesoinStationId { get; set; }

        public int? UtilisateurPlanificationId { get; set; }

        public int? UtilisateurEffectifId { get; set; }

        public DateTime DatePlanifieeDebut { get; set; }

        public DateTime? DatePlanifieeFin { get; set; }

        public DateTime? DateEffectiveDebut { get; set; }

        public DateTime? DateEffectiveFin { get; set; }

        public string TechnicienPlanifie { get; set; }

        public string TechnicienEffectif { get; set; }

        public string Statut { get; set; } 
    }
}
