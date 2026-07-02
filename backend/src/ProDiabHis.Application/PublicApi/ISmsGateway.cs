namespace ProDiabHis.Application.PublicApi;

/// <summary>Abstraction gui SMS OTP</summary>
public interface ISmsGateway
{
    Task SendAsync(string phoneE164, string message, CancellationToken cancellationToken = default);
}
