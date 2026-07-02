using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal;

namespace ProDiabHis.Infrastructure.Persistence;

/// <summary>
/// Workaround Pomelo 8.0.3 + EF Core 8.0.13+ bug:
/// MySqlTypeMappingSource ke thua RelationalTypeMappingSource nhung chua override FindCollectionMapping
/// khi EF Core 8.0.13 goi method nay, base implementation goi elementMapping.ElementType -> NullReferenceException
///
/// Fix: subclass MySqlTypeMappingSource, override FindCollectionMapping de guard null
/// Ref: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1842
/// </summary>
#pragma warning disable EF1001 // Internal EF Core API usage
public class SafeTypeMappingSource : MySqlTypeMappingSource
{
    public SafeTypeMappingSource(
        TypeMappingSourceDependencies dependencies,
        RelationalTypeMappingSourceDependencies relationalDependencies,
        Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal.IMySqlOptions options)
        : base(dependencies, relationalDependencies, options)
    {
    }

    protected override RelationalTypeMapping? FindCollectionMapping(
        RelationalTypeMappingInfo info,
        Type modelType,
        Type? providerType,
        CoreTypeMapping? elementMapping)
    {
        // Guard: elementMapping null se gay NullReferenceException trong base class
        if (elementMapping == null)
            return null;

        try
        {
            return base.FindCollectionMapping(info, modelType, providerType, elementMapping);
        }
        catch (NullReferenceException)
        {
            return null;
        }
    }
}
#pragma warning restore EF1001
