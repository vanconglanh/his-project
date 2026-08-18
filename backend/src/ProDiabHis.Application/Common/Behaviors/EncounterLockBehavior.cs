using MediatR;

namespace ProDiabHis.Application.Common.Behaviors;

/// <summary>
/// [G03] Pipeline behavior chan MOI command ghi len benh an da khoa (status DONE/CANCELLED).
/// Command chi can implement <see cref="IEncounterScopedCommand"/> (co EncounterId) hoac
/// <see cref="IEncounterChildScopedCommand"/> (thao tac tren ban ghi con) la duoc bao ve tu dong —
/// khong phai sua rai rac tung handler, va feature moi khong bi sot.
/// Command implement <see cref="IBypassEncounterLock"/> (vd tao ban dinh chinh) duoc bo qua.
/// </summary>
public class EncounterLockBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEncounterLockGuard _guard;

    public EncounterLockBehavior(IEncounterLockGuard guard) => _guard = guard;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is IBypassEncounterLock)
            return await next();

        Guid? encounterId = null;

        if (request is IEncounterScopedCommand scoped)
        {
            encounterId = scoped.EncounterId;
        }
        else if (request is IEncounterChildScopedCommand child)
        {
            encounterId = await _guard.ResolveEncounterIdAsync(child.ChildKind, child.ChildId, ct);
        }

        if (encounterId is null || encounterId == Guid.Empty)
            return await next();

        var check = await _guard.EnsureEditableAsync(encounterId.Value, ct);
        if (check.IsSuccess)
            return await next();

        return BuildFailure(check.ErrorCode!, check.ErrorMessage!, check.ErrorDetails);
    }

    /// <summary>Dung Result&lt;T&gt;.Failure / Result.Failure tuong ung voi TResponse cua command.</summary>
    private static TResponse BuildFailure(string code, string message, object? details)
    {
        var type = typeof(TResponse);

        if (type == typeof(Result))
            return (TResponse)(object)Result.Failure(code, message, details);

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var factory = type.GetMethod(nameof(Result<object>.Failure),
                new[] { typeof(string), typeof(string), typeof(object) })!;
            return (TResponse)factory.Invoke(null, new object?[] { code, message, details })!;
        }

        // Command khong tra ve Result envelope -> khong the tra loi mem, phai throw de middleware xu ly.
        throw new EncounterLockedException(code, message);
    }
}

/// <summary>Nem khi command khong dung Result envelope ma benh an da khoa.</summary>
public class EncounterLockedException : Exception
{
    public string Code { get; }

    public EncounterLockedException(string code, string message) : base(message) => Code = code;
}
