using System;

namespace StationControl.Models.Station
{
    public class Importance
    {
        public int Id { get; set; }
        public string Libelle { get; set; }
        public decimal Valeur { get; set; }

        public Importance() { }

        public Importance(int id, string libelle, decimal valeur)
        {
            Id = id;
            Libelle = libelle;
            Valeur = valeur;
        }
    }
}
