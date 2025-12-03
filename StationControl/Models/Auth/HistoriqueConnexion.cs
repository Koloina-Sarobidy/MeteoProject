using System;

namespace StationControl.Models.Auth
{
    public class HistoriqueConnexion
    {
        public int Id { get; set; }
        public DateTime DateHeureDebut { get; set; }
        public DateTime? DateHeureFin { get; set; }
        public Utilisateur Utilisateur { get; set; }
    }
}
