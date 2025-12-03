using System;
using StationControl.Models.Inventaire;

namespace StationControl.Models.Station
{
    public class StationBesoinsViewModel
    {
        public Station Station { get; set; }
        public List<InventaireDetail> Besoins { get; set; } = new List<InventaireDetail>();
    } 
}
