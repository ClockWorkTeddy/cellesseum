using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Celleseum.Web.Services;

public sealed class SmtpAccountEmailSender(IOptions<SmtpSettings> options, ILogger<SmtpAccountEmailSender> logger) : IAccountEmailSender
{
    private readonly SmtpSettings _settings = options.Value;

    public async Task<bool> SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        if (!_settings.IsConfigured)
        {
            logger.LogWarning("SMTP is not configured. Skipping confirmation email for {Email}", email);
            return false;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = "Confirm your Celleseum account",
            Body = $"<p>Thanks for registering.</p><p>Please confirm your email by clicking this link:</p><p><a href=\"{WebUtility.HtmlEncode(confirmationLink)}\">Confirm email</a></p>",
            IsBodyHtml = true
        };

        message.To.Add(email);

        using var smtp = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        await smtp.SendMailAsync(message);
        return true;
    }
}
