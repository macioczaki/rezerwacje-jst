using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rezerwacje.Application.Common;

namespace Rezerwacje.Infrastructure.Email;

public class BrevoApiEmailSender : IEmailSender
{
    private readonly BrevoOptions _options;
    private readonly ILogger<BrevoApiEmailSender> _logger;
    private readonly HttpClient _http;

    public BrevoApiEmailSender(
        IOptions<BrevoOptions> options,
        ILogger<BrevoApiEmailSender> logger,
        HttpClient http)
    {
        _options = options.Value;
        _logger = logger;
        _http = http;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Brevo:ApiKey is not configured.");

        var payload = new
        {
            sender = new { name = _options.FromName, email = _options.FromEmail },
            to = new[] { new { email = to } },
            subject,
            htmlContent = htmlBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl);
        request.Headers.Add("api-key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = JsonContent.Create(payload);

        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Brevo API zwróciło {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException(
                $"Nie udało się wysłać maila (Brevo API: {response.StatusCode}).");
        }

        _logger.LogInformation("Email wysłany przez Brevo API do {To} z tematem {Subject}", to, subject);
    }
}