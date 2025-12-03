using StationControl.Models.Auth;

namespace StationControl.Models.Personnel
{
    public class DemandesInscriptionViewModel
    {
        public List<Utilisateur> DemandesCRM { get; set; }
        public List<Utilisateur> DemandesSuperAdmin { get; set; }
        public List<Utilisateur> DemandesObservateurs { get; set; }
        public Dictionary<int, string> Crms { get; set; }
        public Dictionary<int, string> Stations { get; set; }
    }
}
