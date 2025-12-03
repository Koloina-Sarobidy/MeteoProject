using System;
using StationControl.Models.Station;
using StationControl.Models.Besoin;

namespace StationControl.Models.Inventaire
{
    public class BesoinStation
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string DescriptionProbleme { get; set; }
        public bool EstRenvoye { get; set; } = false;
        public bool EstTraite { get; set; } = false;
        public Station.Station Station { get; set; }
        public int EquipementStationId { get; set; }
        public EquipementStation EquipementStation { get; set; }
        public EquipementBesoin EquipementBesoin { get; set; }
    }
}
