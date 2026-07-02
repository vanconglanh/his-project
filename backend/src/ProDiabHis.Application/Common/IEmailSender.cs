namespace ProDiabHis.Application.Common;

/// <summary>Dich vu gui email (dev = SMTP, prod = SendGrid)</summary>
public interface IEmailSender
{
    /// <summary>Gui email HTML toi dia chi to</summary>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
