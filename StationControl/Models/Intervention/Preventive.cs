using StationControl.Models.Auth;
using System;

namespace StationControl.Models.Intervention
{
    public class Preventive
    {
        public int Id { get; set; }

        public Station.Station Station { get; set; }             
        public Utilisateur UtilisateurPlanificateur { get; set; }

        public Utilisateur? UtilisateurEffectif { get; set; }

        public DateTime DatePrevueDebut { get; set; }
        public DateTime? DatePrevueFin { get; set; }

        public DateTime? DateEffectiveDebut { get; set; }
        public DateTime? DateEffectiveFin { get; set; }

        public bool EstComplete { get; set; } = false;
        public bool EstAnnule { get; set; } = false;
        public string Description { get; set; } = string.Empty;
    }
}
