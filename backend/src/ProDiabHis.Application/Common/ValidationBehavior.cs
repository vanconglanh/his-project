using FluentValidation;
using MediatR;

namespace ProDiabHis.Application.Common;

/// <summary>
/// Pipeline behavior chay tat ca FluentValidation validator dang ky cho TRequest
/// truoc khi handler thuc thi. Neu co loi, throw FluentValidation.ValidationException
/// de ErrorHandlingMiddleware bat va tra ve HTTP 400 voi envelope chuan
/// { "error": { "code": "VALIDATION_ERROR", "message": "...", "details": {...} } }.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
