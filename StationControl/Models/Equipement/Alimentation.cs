using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace StationControl.Models.Equipement
{
    public class Alimentation
    {
        public int Id { get; set; }
        public string Libelle { get; set; }
        public string Description { get; set; }

        public Alimentation() { }

        public Alimentation(int id, string libelle, string description)
        {
            Id = id;
            Libelle = libelle;
            Description = description;
        }

        public Alimentation(string libelle, string description)
        {
            Libelle = libelle;
            Description = description;
        }
    }
}
