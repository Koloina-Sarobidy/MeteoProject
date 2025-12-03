using System;

namespace StationControl.Models.Station
{
    public class TypeStation
    {
        public int Id { get; set; }
        public string Libelle { get; set; }
        public string? Description { get; set; }

        public TypeStation() { }

        public TypeStation(int id, string libelle, string? description)
        {
            Id = id;
            Libelle = libelle;
            Description = description;
        }
    }
}
