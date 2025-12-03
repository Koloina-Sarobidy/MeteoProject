using System;

namespace StationControl.Models.Station
{
    public class Brand
    {
        public int Id { get; set; }
        public string Nom { get; set; }

        public Brand() { }

        public Brand(int id, string nom)
        {
            Id = id;
            Nom = nom;
        }
    }
}
