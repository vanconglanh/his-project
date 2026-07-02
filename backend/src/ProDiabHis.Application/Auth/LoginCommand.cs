using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Contracts.Auth;

namespace ProDiabHis.Application.Auth;

/// <summary>Command dang nhap he thong</summary>
public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;
