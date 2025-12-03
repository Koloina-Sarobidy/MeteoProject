using System;
using MySql.Data.MySqlClient;

namespace StationControl.Models.Auth
{
    public class Utilisateur
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string MotDePasse { get; set; }
        public Role Role { get; set; }
        public int? StationId { get; set; }
        public int? CrmId { get; set; }
        public string Genre { get; set; }
        public bool EstValide { get; set; } = true;
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public string? PhotoProfil { get; set; }
        public string Statut { get; set; }
    }
}
