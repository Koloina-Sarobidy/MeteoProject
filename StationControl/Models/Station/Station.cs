using StationControl.Models.Crm;

namespace StationControl.Models.Station
{
    public class Station
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public TypeStation TypeStation { get; set; }
        public Brand Brand { get; set; }  
        public Crm.Crm Crm { get; set; }  
        public Region Region { get; set; }
        public string? Statut { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public bool? EstArrete { get; set; } = false;

        public Station() { }

        public Station(int id, string nom, decimal? latitude, decimal? longitude, TypeStation typeStation, Crm.Crm crm,
                       Brand brand, Region region, string? statut, DateTime dateDebut, DateTime? dateFin,
                       bool? estArrete)
        {
            Id = id;
            Nom = nom;
            Latitude = latitude;
            Longitude = longitude;
            TypeStation = typeStation;
            Brand = brand;
            Crm = crm;
            Region = region;
            Statut = statut;
            DateDebut = dateDebut;
            DateFin = dateFin;
            EstArrete = estArrete;
        }
    }
}
