using System.Collections.Generic;

namespace StationControl.Models.Util
{
    public class PredictionBesoinParType
    {
        public int EquipementBesoinId { get; set; }
        public string EquipementBesoinLibelle { get; set; }
        public DateTime? DerniereOccurrence { get; set; }
        public double? FrequenceMoyenneJours { get; set; } 
        public DateTime? DateProchaineOccurrence { get; set; } 
        public bool AUtiliseFallback { get; set; } 
    }

    public class PredictionBesoinMulti
    {
        public int EquipementStationId { get; set; }
        public DateTime? DernierBesoinGlobal { get; set; }
        public double? FrequenceMoyenneGlobalJours { get; set; } 
        public List<PredictionBesoinParType> ParType { get; set; } = new List<PredictionBesoinParType>();
    }
}
