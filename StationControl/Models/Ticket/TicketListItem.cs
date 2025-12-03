namespace StationControl.Models.Ticket
{
    public class TicketListItem
    {
        public int Id { get; set; }
        public string Objet { get; set; }
        public string Description { get; set; }
        public DateTime DateCreation { get; set; }
        public bool DejaVu { get; set; }
        public Crm.Crm? Crm { get; set; }
        public Station.Station? Station { get; set; }
        public bool? SuperAdmin { get; set; }
        public List<Crm.Crm> CrmDestinataire { get; set; }
        public List<Station.Station> StationDestinataire { get; set; }
        public bool? SuperAdminDestinataire { get; set; }
    }
}
