namespace ProDiabHis.Application.Common;

/// <summary>1 file dinh kem email (dung cho lich gui bao cao dinh ky — Report Builder P3.3).</summary>
public record EmailAttachment(string FileName, byte[] Content, string ContentType);

/// <summary>Dich vu gui email (dev = SMTP, prod = SendGrid)</summary>
public interface IEmailSender
{
    /// <summary>Gui email HTML toi dia chi to</summary>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>Gui email HTML kem file dinh kem (vd PDF/Excel bao cao dinh ky) toi dia chi to.</summary>
    Task SendWithAttachmentAsync(string to, string subject, string htmlBody, EmailAttachment attachment, CancellationToken cancellationToken = default);
}
