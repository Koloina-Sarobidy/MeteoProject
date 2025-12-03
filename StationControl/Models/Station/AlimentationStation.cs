using System;
using StationControl.Models.Equipement;

namespace StationControl.Models.Station
{
    public class AlimentationStation
    {
        public int Id { get; set; }
        public Station Station { get; set; } = new Station();
        public Alimentation Alimentation{ get; set; } = new Alimentation();
        public string NumSerie { get; set; } = string.Empty;
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public decimal? EstimationVieAnnee { get; set; }
        public string Statut { get; set; } = "Fonctionnel";
        public bool? EstRemplace { get; set; }
    }
}
