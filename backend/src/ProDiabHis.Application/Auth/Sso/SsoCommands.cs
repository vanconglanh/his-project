using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Auth.Sso;

public record StartSsoLoginCommand(string? ReturnUrl) : IRequest<Result<SsoStartResult>>;

public record CompleteSsoLoginCommand(string? Code, string? State, string? Error, string? BindCookie,
    string? IpAddress, string? UserAgent) : IRequest<Result<SsoCompleteResult>>;

public record SsoLogoutCommand(string? RefreshToken, string? IpAddress, string? UserAgent)
    : IRequest<Result<SsoLogoutResult>>;

public record SsoBackchannelLogoutCommand(string? LogoutToken, string? IpAddress)
    : IRequest<Result<int>>;

public record RemoveExternalLoginCommand(Guid UserId, string Provider) : IRequest<Result>;
