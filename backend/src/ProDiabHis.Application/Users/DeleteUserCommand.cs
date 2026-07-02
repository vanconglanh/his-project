using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Users;

public record DeleteUserCommand(Guid Id) : IRequest<Result>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _currentUser;

    public DeleteUserCommandHandler(IApplicationDbContext db, IAuditService audit, ICurrentUser currentUser)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteUserCommand req, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == req.Id && u.DeletedAt == null, ct);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = _currentUser.UserId;
        user.IsActive = false;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Delete, "user", req.Id.ToString(), null, ct);

        return Result.Success();
    }
}
