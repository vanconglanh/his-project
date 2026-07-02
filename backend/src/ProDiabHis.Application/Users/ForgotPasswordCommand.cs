using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Users;

/// <summary>Gui email reset password. Luon tra ve thanh cong de chong enumeration.</summary>
public record ForgotPasswordCommand(string Email) : IRequest<Result>;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordCommandHandler(IApplicationDbContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    public async Task<Result> Handle(ForgotPasswordCommand req, CancellationToken ct)
    {
        // Luon tra thanh cong de chong enumeration
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == req.Email && u.DeletedAt == null, ct);

        if (user != null)
        {
            var resetToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLower();
            user.PasswordResetToken = resetToken;
            user.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(2);
            await _db.SaveChangesAsync(ct);

            var resetUrl = $"https://app.prodiab.vn/reset-password?token={resetToken}";
            var emailBody = $"""
                <html>
                <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                  <h2>Xin chào {user.FullName},</h2>
                  <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu của bạn trên hệ thống Pro-Diab HIS.</p>
                  <p>Vui lòng nhấn nút bên dưới để đặt lại mật khẩu:</p>
                  <a href="{resetUrl}" style="
                      display: inline-block;
                      padding: 12px 24px;
                      background-color: #dc2626;
                      color: white;
                      text-decoration: none;
                      border-radius: 6px;
                      margin: 16px 0;">
                    Đặt lại mật khẩu
                  </a>
                  <p style="color: #6b7280; font-size: 14px;">Liên kết này có hiệu lực trong 2 giờ.</p>
                  <p style="color: #6b7280; font-size: 14px;">Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
                  <hr/>
                  <p style="color: #9ca3af; font-size: 12px;">Pro-Diab HIS — Hệ thống quản lý phòng khám</p>
                </body>
                </html>
                """;

            await _emailSender.SendAsync(req.Email, "Đặt lại mật khẩu Pro-Diab HIS", emailBody, ct);
        }

        // Luon tra thanh cong
        return Result.Success();
    }
}
