using System;

namespace StationControl.Models.Station
{
    public class EquipementStation
    {
        public int Id { get; set; }

        public string NumSerie { get; set; } = "";

        public string Statut { get; set; } = "Inconnu";

        public int? CapteurId { get; set; }
        public int? AlimentationId { get; set; }

        public Station? Station { get; set; }

        public bool EstAlimentation { get; set; } = false;

        public DateTime DateDebut { get; set; } = DateTime.MinValue;
        public DateTime? DateFin { get; set; }

        public Importance? Importance { get; set; }

        public decimal? EstimationCout { get; set; }
    }
}
