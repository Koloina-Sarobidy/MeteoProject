using System;
using System.Collections.Generic;
using StationControl.Models.Besoin;

namespace StationControl.Models.Inventaire
{
    public class Inventaire
    {
        public int Id { get; set; }
        public int StationId { get; set; }
        public int UtilisateurId { get; set; }
        public DateTime DateInventaire { get; set; } = DateTime.Now;
        public List<InventaireDetail> Details { get; set; } = new List<InventaireDetail>();
        public string Commentaire { get; set; }
    }

    public class InventaireDetail
    {
        public int Id { get; set; }
        public int InventaireId { get; set; }
        public int EquipementStationId { get; set; }
        public bool EstFonctionnel { get; set; } = true;
        public BesoinStation BesoinStation { get; set; }  
        public Inventaire Inventaire { get; set; }
        public object EquipementStation { get; internal set; }
    }
}
