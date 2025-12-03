namespace StationControl.Models.Auth
{
    public class UtilisateurStatutHistorique
    {
        public int Id { get; set; }
        public Utilisateur Utilisateur { get; set; } = null;
        public string Statut { get; set; } = null!; 
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public Utilisateur? UtilisateurMaj { get; set; }
        public string? Description { get; set; }
    }
}
