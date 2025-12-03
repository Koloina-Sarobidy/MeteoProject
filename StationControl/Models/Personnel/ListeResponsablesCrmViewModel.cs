using StationControl.Models.Auth;

namespace StationControl.Models.Personnel
{
    public class ListeResponsablesCrmViewModel
    {
        public List<Utilisateur> Responsables { get; set; }
        public List<Crm.Crm> Crms { get; set; }

        public int? CrmId { get; set; }
        public DateTime? DateDebut { get; set; }
        public string Statut { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages => (int)Math.Ceiling((decimal)TotalCount / PageSize);
    }
}
