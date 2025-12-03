using System;

namespace StationControl.Models.Rapport
{
    public class RapportMensuelStation
    {
        // Station
        public string StationNom { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string TypeStation { get; set; }
        public string Region { get; set; }

        // Intervention
        public DateTime? DatePlanifieeDebut { get; set; }
        public DateTime? DatePlanifieeFin { get; set; }
        public DateTime? DateEffectiveDebut { get; set; }
        public DateTime? DateEffectiveFin { get; set; }
        public string StatutIntervention { get; set; }
        public string TechnicienPlanifie { get; set; }
        public string TechnicienEffectif { get; set; }

        // Besoin
        public string BesoinDescription { get; set; }
        public string EquipementBesoinLibelle { get; set; }

        // Equipement
        public string EquipementNumSerie { get; set; }
        public string EquipementLibelle { get; set; }
        public string EquipementStatut { get; set; }
        public bool? EstAlimentation { get; set; }
    }
}
