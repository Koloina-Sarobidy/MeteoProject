using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

public class NotificationsViewComponent : ViewComponent
{
    private readonly IConfiguration _configuration;

    public NotificationsViewComponent(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IViewComponentResult Invoke(int? crmId = null, int? stationId = null, bool superAdmin = false)
    {
        int ticketsNonVus = 0;
        string connStr = _configuration.GetConnectionString("DefaultConnection");

        using var conn = new MySqlConnection(connStr);
        conn.Open();

        if (superAdmin)
        {
            ticketsNonVus = StationControl.Areas.SuperAdmin.Services.Ticket.TicketService.GetTicketsNonVusPourSuperAdmin(conn).Count;
        }
        else if (crmId.HasValue)
        {
            ticketsNonVus = StationControl.Areas.Admin.Services.Ticket.TicketService.GetTicketsNonVusPourCrm(conn, crmId.Value).Count;
        }
        else if (stationId.HasValue)
        {
            ticketsNonVus = StationControl.Areas.Observateur.Services.Ticket.TicketService.GetTicketsNonVusPourStation(conn, stationId.Value).Count;
        }

        return Content($"{ticketsNonVus}");
    }
}
