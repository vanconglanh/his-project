using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Email;

/// <summary>Gui email qua SMTP dung MailKit. Dev = MailHog, Prod = SendGrid SMTP relay</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Gui email HTML</summary>
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        => SendCoreAsync(to, subject, htmlBody, attachment: null, cancellationToken);

    /// <summary>Gui email HTML kem file dinh kem (vd PDF/Excel bao cao dinh ky — Report Builder P3.3).</summary>
    public Task SendWithAttachmentAsync(string to, string subject, string htmlBody, EmailAttachment attachment, CancellationToken cancellationToken = default)
        => SendCoreAsync(to, subject, htmlBody, attachment, cancellationToken);

    private async Task SendCoreAsync(string to, string subject, string htmlBody, EmailAttachment? attachment, CancellationToken cancellationToken)
    {
        var host = _configuration["Smtp:Host"] ?? "localhost";
        var port = _configuration.GetValue<int>("Smtp:Port", 1025);
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:FromEmail"] ?? "no-reply@prodiab.vn";
        var fromName = _configuration["Smtp:FromName"] ?? "Pro-Diab HIS";
        var useSsl = _configuration.GetValue<bool>("Smtp:UseSsl", false);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        if (attachment is not null)
            bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var socketOptions = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
            await client.ConnectAsync(host, port, socketOptions, cancellationToken);

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                await client.AuthenticateAsync(username, password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email gui thanh cong den {To}, chu de: {Subject}, co dinh kem: {HasAttachment}", to, subject, attachment is not null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi gui email den {To}", to);
            // Khong throw — email gui loi khong nen lam hong luong chinh
        }
    }
}
