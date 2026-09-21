namespace Rezerwacje.Infrastructure.Email;

public class BrevoOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Rezerwacje JST";
    public string ApiUrl { get; set; } = "https://api.brevo.com/v3/smtp/email";
}