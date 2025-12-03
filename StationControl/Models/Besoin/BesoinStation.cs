namespace StationControl.Models.Besoin
{
    public class BesoinStation
    {
        public int Id { get; set; }
        public int EquipementStationId { get; set; }
        public DateTime Date { get; set; }
        public string DescriptionProbleme { get; set; }
        public bool EstRenvoye { get; set; } = false;
        public bool EstTraite { get; set; } = false;
        public bool EstComplete { get; set; } = false;
        public Station.Station Station { get; set; }
        public Station.AlimentationStation AlimentationStation { get; set; }
        public Station.CapteurStation CapteurStation { get; set; }
        public EquipementBesoin EquipementBesoin { get; set; }

    }
}
