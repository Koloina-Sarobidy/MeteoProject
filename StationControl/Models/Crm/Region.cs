using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace StationControl.Models.Crm
{
    public class Region
    {
        public int Id { get; set; }
        public string Libelle { get; set; }

        public Region() {}

        public Region(int id, string libelle)
        {
            Id = id;
            Libelle = libelle;
        }
    }
}
