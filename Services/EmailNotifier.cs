using System.Net;
using System.Net.Mail;
using FiscalisationService.Models;

namespace FiscalisationService.Services;

public sealed class EmailNotifier
{
    private DateTimeOffset? _lastSent;
    private readonly object _lock = new();

    public async Task NotifyTimeoutAsync(ServiceConfig config, Exception? exception, CancellationToken cancellationToken)
    {
        var settings = config.EmailSettings;
        if (!settings.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.SmtpHost) || string.IsNullOrWhiteSpace(settings.ToAddresses))
        {
            return;
        }

        if (!ShouldSend(settings.ThrottleMinutes))
        {
            return;
        }

        var fromAddress = string.IsNullOrWhiteSpace(settings.FromAddress)
            ? settings.SmtpUser
            : settings.FromAddress;

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress),
            Subject = settings.Subject,
            Body = BuildBody(config, exception),
            IsBodyHtml = false
        };

        foreach (var address in SplitAddresses(settings.ToAddresses))
        {
            message.To.Add(address);
        }

        if (message.To.Count == 0)
        {
            return;
        }

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(settings.SmtpUser))
        {
            client.Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPassword);
        }

        await client.SendMailAsync(message, cancellationToken);
    }

    private bool ShouldSend(int throttleMinutes)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            if (_lastSent.HasValue && now - _lastSent.Value < TimeSpan.FromMinutes(Math.Max(1, throttleMinutes)))
            {
                return false;
            }

            _lastSent = now;
            return true;
        }
    }

    private static IEnumerable<string> SplitAddresses(string addresses)
    {
        return addresses
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item));
    }

    private static string BuildBody(ServiceConfig config, Exception? exception)
    {
        var lines = new List<string>
        {
            "API timeout detected. You may experience issues with deal note fiscalisation.",
            $"Time (UTC): {DateTimeOffset.UtcNow:O}",
            $"Client: {config.EmailSettings.ClientName}",
            ""
        }; 

        if (exception is not null)
        {
            lines.Add("Exception:");
            lines.Add(exception.ToString());
        }

        return string.Join(Environment.NewLine, lines);
    }
}
