using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Application.Tenants;

/// <summary>Lay thong tin letterhead cua tenant hien tai de in len PDF bao cao.</summary>
public record GetLetterheadQuery : IRequest<Result<LetterheadDto>>;

public class GetLetterheadQueryHandler : IRequestHandler<GetLetterheadQuery, Result<LetterheadDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenantProvider;

    public GetLetterheadQueryHandler(IDapperConnectionFactory db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<LetterheadDto>> Handle(GetLetterheadQuery req, CancellationToken ct)
    {
        var tenantId = _tenantProvider.TenantId;

        using var conn = _db.CreateConnection();

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT name, cskcb_code, company_name, logo_url, address, phone, email, email_support
              FROM diab_his_sys_tenants
              WHERE id = @tenantId AND deleted_at IS NULL
              LIMIT 1",
            new { tenantId });

        if (row is null)
            return Result<LetterheadDto>.Failure("TENANT_NOT_FOUND", "Không tìm thấy thông tin phòng khám");

        var dto = new LetterheadDto(
            ClinicName: (string)(row.name ?? ""),
            CskcbCode: (string?)row.cskcb_code,
            CompanyName: (string?)row.company_name,
            Address: (string?)row.address,
            Phone: (string?)row.phone,
            Email: (string?)row.email,
            EmailSupport: (string?)row.email_support,
            LogoUrl: (string?)row.logo_url);

        return Result<LetterheadDto>.Success(dto);
    }
}
