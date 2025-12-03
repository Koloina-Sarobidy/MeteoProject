using System;
using System.Collections.Generic;
using StationControl.Models.Ticket;

namespace StationControl.Models.Ticket
{
    public class TicketsSuperAdminViewModel
    {
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }

        public List<TicketListItem> TicketsEnvoyes { get; set; } = new();
        public List<TicketListItem> TicketsRecus { get; set; } = new();
    }
}
