namespace Rezerwacje.Infrastructure.Email;

public static class EmailTemplates
{
    public static string PasswordReset(string resetLink, int validMinutes)
    {
        return $@"
<!DOCTYPE html>
<html lang=""pl"">
<body style=""font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
  <h1 style=""color: #1d4ed8;"">Reset hasła</h1>
  <p>Otrzymaliśmy prośbę o zresetowanie hasła do Twojego konta w systemie <strong>Rezerwacje JST</strong>.</p>
  <p>Aby ustawić nowe hasło, kliknij poniższy link:</p>
  <p style=""text-align: center; margin: 30px 0;"">
    <a href=""{resetLink}""
       style=""background-color: #1d4ed8; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;"">
      Ustaw nowe hasło
    </a>
  </p>
  <p>Link jest ważny przez <strong>{validMinutes} minut</strong>.</p>
  <p>Jeśli to nie Ty prosiłeś o reset hasła, zignoruj tę wiadomość — Twoje hasło pozostanie niezmienione.</p>
  <hr style=""border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;"">
  <p style=""font-size: 12px; color: #9ca3af;"">
    Ta wiadomość została wygenerowana automatycznie. Nie odpowiadaj na nią.
  </p>
</body>
</html>";
    }
}