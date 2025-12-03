using System;
using System.Collections.Generic;

namespace StationControl.Models.Util
{
    public class StationComplet
    {
        public string Crm { get; set; }
        public string Region { get; set; }
        public string CityName { get; set; }
        public string Type { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? Latitude { get; set; }
        public string Etat { get; set; }
        public string ManufacturerBrand { get; set; }
        public string ActionAEntreprendre { get; set; }
        public int EquipementStationId { get; set; }
        public string EquipementLibelle { get; set; }
        public string EquipementStatut { get; set; }
        public bool EstAlimentation { get; set; }
    }
}

