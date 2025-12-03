using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using StationControl.Models.Auth;
using MySql.Data.MySqlClient;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendResetCode(string toEmail, string code)
    {
        var emailSettings = _config.GetSection("EmailSettings");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("StationControl", emailSettings["SenderEmail"]));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = "Réinitialisation de mot de passe";

        message.Body = new TextPart("plain")
        {
            Text = $"Voici votre code de réinitialisation pour StationControl: {code}\nIl expire dans 15 minutes."
        };

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(
                emailSettings["SmtpServer"],
                int.Parse(emailSettings["Port"]),
                MailKit.Security.SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(
                emailSettings["SenderEmail"],
                emailSettings["SenderPassword"]
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
    public static async Task SendEmail(IConfiguration _config, Utilisateur utilisateur, string subject, string texte)
    {
        if (utilisateur == null)
            throw new ArgumentNullException(nameof(utilisateur));
        if (string.IsNullOrEmpty(utilisateur.Email))
            throw new ArgumentException("L'utilisateur doit avoir un email", nameof(utilisateur));

        var emailSettings = _config.GetSection("EmailSettings");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("StationControl", emailSettings["SenderEmail"]));
        message.To.Add(new MailboxAddress("", utilisateur.Email));
        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = $"Bonjour {utilisateur.Prenom},\n\n{texte}"
        };

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(
                emailSettings["SmtpServer"],
                int.Parse(emailSettings["Port"]),
                MailKit.Security.SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(
                emailSettings["SenderEmail"],
                emailSettings["SenderPassword"]
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
