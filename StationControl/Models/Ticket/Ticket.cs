using System;
using System.Collections.Generic;

namespace StationControl.Models.Ticket
{
    public class Ticket
    {
        public int Id { get; set; }
        public string Objet { get; set; }
        public string Description { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public int UtilisateurId { get; set; }
        public int? CrmId { get; set; }
        public int? StationId { get; set; }
        public bool? SuperAdmin { get; set; }

        public List<TicketPieceJointe> PiecesJointes { get; set; } = new();
        public List<TicketVisibilite> Visibilites { get; set; } = new();
        public List<TicketVue> Vues { get; set; } = new();
    }

    public class TicketPieceJointe
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Url { get; set; }
    }

    public class TicketVisibilite
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public bool? SuperAdmin { get; set; }
        public int? CrmId { get; set; }
        public Crm.Crm Crm { get; set; }
        public int? StationId { get; set; }
        public Station.Station Station { get; set; }
    }

    public class TicketVue
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public bool? SuperAdmin { get; set; }
        public Models.Crm.Crm? CrmId { get; set; }
        public Station.Station? StationId { get; set; }
        public DateTime DateVue { get; set; } = DateTime.Now;
    }
}
