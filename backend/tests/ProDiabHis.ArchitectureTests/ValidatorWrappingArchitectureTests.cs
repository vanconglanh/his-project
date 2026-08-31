using System.Reflection;
using FluentAssertions;
using Xunit;

namespace ProDiabHis.ArchitectureTests;

/// <summary>
/// BUG-04 (chan tai phat): moi MediatR *Command boc mot property `Request` — neu KIEU cua
/// Request da co validator (AbstractValidator&lt;TRequest&gt;) thi BAT BUOC phai co validator
/// cap Command (AbstractValidator&lt;TCommand&gt;) de MediatR ValidationBehavior thuc su chay.
///
/// Loi goc BUG-04: CreatePaymentValidator : AbstractValidator&lt;CreatePaymentRequest&gt; ton tai
/// nhung KHONG co CreatePaymentCommandValidator -> pipeline resolve IValidator&lt;CreatePaymentCommand&gt;
/// = rong -> validator "chet" -> so tien 0/am/vuot deu lot qua. Test nay bat lop loi do o cap CI.
/// </summary>
public class ValidatorWrappingArchitectureTests
{
    private const string AbstractValidatorFullName = "FluentValidation.AbstractValidator`1";

    [Fact]
    public void EveryCommand_WrappingAValidatedRequest_MustHave_CommandLevelValidator()
    {
        var asm = typeof(ProDiabHis.Application.DependencyInjection).Assembly;
        var allTypes = asm.GetTypes();

        // 1) Tap cac kieu DA co validator (T trong AbstractValidator<T>).
        var validatedTypes = new HashSet<Type>();
        foreach (var t in allTypes)
        {
            for (var bt = t.BaseType; bt != null; bt = bt.BaseType)
            {
                if (bt.IsGenericType &&
                    bt.GetGenericTypeDefinition().FullName == AbstractValidatorFullName)
                {
                    validatedTypes.Add(bt.GetGenericArguments()[0]);
                    break;
                }
            }
        }

        // 2) Cac *Command (MediatR IRequest) co property `Request` ma KIEU cua Request da co validator,
        //    nhung ban than Command lai CHUA co validator -> vi pham.
        var violations = new List<string>();
        foreach (var cmd in allTypes)
        {
            if (cmd.IsAbstract || cmd.IsInterface) continue;

            var isRequest = cmd.GetInterfaces().Any(i =>
                i.FullName != null && i.FullName.StartsWith("MediatR.IRequest"));
            if (!isRequest) continue;

            var requestProp = cmd.GetProperty("Request",
                BindingFlags.Public | BindingFlags.Instance);
            if (requestProp == null) continue;

            if (!validatedTypes.Contains(requestProp.PropertyType)) continue;

            if (!validatedTypes.Contains(cmd))
                violations.Add(
                    $"{cmd.Name} boc {requestProp.PropertyType.Name} (da co validator) " +
                    $"nhung THIEU validator cap Command -> validator se KHONG chay.");
        }

        violations.Should().BeEmpty(
            because: "moi Command boc request da validate PHAI co lop validator cap Command " +
                     "(RuleFor(x => x.Request).SetValidator(...)), neu khong FluentValidation se bi bo qua:\n"
                     + string.Join("\n", violations));
    }
}
