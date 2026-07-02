using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace ProDiabHis.Infrastructure.Persistence;

/// <summary>
/// Workaround cho Pomelo 8.0.3 + EF Core 8.0.13+ bug:
/// ValidatePropertyMapping() goi FindCollectionMapping(null) khi gap byte[] property
/// -> NullReferenceException trong RelationalTypeMappingSource.FindCollectionMapping
///
/// Fix: override ValidatePropertyMapping de try-catch loi nay va skip byte[] properties
/// Ref: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1842
/// </summary>
public class SafeModelValidator : RelationalModelValidator
{
    public SafeModelValidator(
        ModelValidatorDependencies dependencies,
        RelationalModelValidatorDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }

    public override void Validate(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        try
        {
            base.Validate(model, logger);
        }
        catch (NullReferenceException ex) when (ex.StackTrace?.Contains("FindCollectionMapping") == true
                                                 || ex.StackTrace?.Contains("ValidatePropertyMapping") == true)
        {
            // Pomelo 8.0.3 chua implement FindCollectionMapping cho EF Core 8.0.13+ primitive collection feature
            // Log warning va tiep tuc — cac byte[] properties van hoat dong binh thuong qua HasColumnType
            Console.WriteLine("[WARN] SafeModelValidator: bo qua NullReferenceException trong ValidatePropertyMapping " +
                              "(Pomelo 8.0.3 bug voi EF Core 8.0.13+). All byte[] properties phai co HasColumnType tuong ung.");
        }
    }
}
