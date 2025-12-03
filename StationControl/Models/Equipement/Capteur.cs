using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace StationControl.Models.Equipement
{
    public class Capteur
    {
        public int Id { get; set; }
        public string Libelle { get; set; }
        public string Parametre { get; set; }

        public Capteur() { }

        public Capteur(int id, string libelle, string parametre)
        {
            Id = id;
            Libelle = libelle;
            Parametre = parametre;
        }

        public Capteur(string libelle, string parametre)
        {
            Libelle = libelle;
            Parametre = parametre;
        }
    }
}
