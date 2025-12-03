using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace StationControl.Models.Crm
{
    public class Crm
    {
        public int Id { get; set; }
        public string Libelle { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }

        public DateTime? DateMaj { get; set; }
        public bool? EstArrete { get; set; }
        public Crm() { }

        public Crm(int id, string libelle, DateTime dateDebut, DateTime? dateFin, DateTime? dateMaj, bool? estArrete)
        {
            Id = id;
            Libelle = libelle;
            DateDebut = dateDebut;
            DateFin = dateFin;
            DateMaj = dateMaj;
            EstArrete = estArrete;
        }
    }
}
