namespace Rezerwacje.Infrastructure.Email;

public class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "noreply@rezerwacje-jst.local";
    public string FromName { get; set; } = "Rezerwacje JST";
}