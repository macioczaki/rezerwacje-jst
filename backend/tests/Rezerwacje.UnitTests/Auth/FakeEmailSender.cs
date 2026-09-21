using Rezerwacje.Application.Common;

namespace Rezerwacje.UnitTests.Auth;

/// <summary>
/// Mock IEmailSender dla testów — nic nie wysyła, tylko zapamiętuje wywołania.
/// </summary>
public class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = new();

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        Sent.Add((to, subject, htmlBody));
        return Task.CompletedTask;
    }
}