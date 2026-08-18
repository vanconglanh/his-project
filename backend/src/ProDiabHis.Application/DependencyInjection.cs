using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Kich hoat FluentValidation trong pipeline MediatR — thieu dong nay thi
        // moi validator dang ky se KHONG bao gio duoc chay (dead-code).
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // [G03] Chan moi command ghi len benh an da khoa (DONE/CANCELLED).
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviors.EncounterLockBehavior<,>));

        return services;
    }
}
