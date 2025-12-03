using StationControl.Models.Intervention;
using StationControl.Models.Auth;
using System.Collections.Generic;
using System;

namespace StationControl.Models.Intervention
{
    public class PreventiveFiltre
    {
        public string? StationNom { get; set; }
        public DateTime? DatePrevueDebut { get; set; }
        public DateTime? DateEffectiveDebut { get; set; }
        public int? PlanificateurId { get; set; }
        public int? EffectifId { get; set; }
    }

    public class PreventiveViewModel
    {
        public List<Preventive> Interventions { get; set; } = new();
        public PreventiveFiltre Filtre { get; set; } = new();
        public List<Utilisateur> Planificateurs { get; set; } = new();
        public List<Utilisateur> Effectifs { get; set; } = new();

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public DateTime DatePrevueDebut { get; internal set; }
    }
}
