using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Interfaces;

namespace ProDiabHis.Application.Patients;

// ────────────────────────────────────────────────
// §4.7.3 — GET /api/v1/patients/{id}/external-pathway
// LUON tra HTTP 200 kem data.status; KHONG BAO GIO chan luong kham.
// ────────────────────────────────────────────────
public record GetExternalPathwayQuery(Guid PatientId, bool Force)
    : IRequest<Result<ExternalPathwayResponse>>;

/// <summary>Response khoi lo trinh he ngoai — theo dung format §4.7.3.</summary>
public record ExternalPathwayResponse(
    string Status,                    // OK | NOT_FOUND | UNAVAILABLE | NOT_CONFIGURED
    string? PackageName,
    string? DisplayLabel,
    int? CurrentWeek,
    int? TotalWeeks,
    IReadOnlyList<ExternalPathwayMilestoneDto> Milestones,
    DateTime FetchedAt,
    bool FromCache,
    string? ErrorMessage);

public record ExternalPathwayMilestoneDto(int Week, string State);

public class GetExternalPathwayQueryHandler
    : IRequestHandler<GetExternalPathwayQuery, Result<ExternalPathwayResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IExternalPathwayProvider _provider;

    public GetExternalPathwayQueryHandler(IApplicationDbContext db, ITenantProvider tenant,
        IExternalPathwayProvider provider)
    { _db = db; _tenant = tenant; _provider = provider; }

    public async Task<Result<ExternalPathwayResponse>> Handle(GetExternalPathwayQuery q, CancellationToken ct)
    {
        // Lay dinh danh benh nhan de tra cuu he ngoai (SDT). NullProvider bo qua tham so nay.
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == q.PatientId, ct);

        var query = new ExternalPathwayQuery(
            TenantId: _tenant.TenantId,
            Phone: patient?.Phone,
            CitizenId: null,
            ExternalAccountId: null);

        // Provider bao dam khong nem loi (graceful degradation §4.7.3). Bao ve them 1 lop o day.
        ExternalPathwayResult result;
        try
        {
            result = await _provider.GetPathwayAsync(query, ct);
        }
        catch
        {
            result = new ExternalPathwayResult(
                ExternalPathwayStatus.Unavailable, null, null, null, null, null, null,
                Array.Empty<ExternalPathwayMilestone>(), DateTime.UtcNow,
                "Không lấy được dữ liệu lộ trình");
        }

        var response = new ExternalPathwayResponse(
            Status: MapStatus(result.Status),
            PackageName: result.PackageName,
            DisplayLabel: result.DisplayLabel,
            CurrentWeek: result.CurrentWeek,
            TotalWeeks: result.TotalWeeks,
            Milestones: result.Milestones
                .Select(m => new ExternalPathwayMilestoneDto(m.Week, m.State.ToUpperInvariant()))
                .ToList(),
            FetchedAt: result.FetchedAt,
            FromCache: false,
            ErrorMessage: result.ErrorMessage);

        return Result<ExternalPathwayResponse>.Success(response);
    }

    private static string MapStatus(ExternalPathwayStatus s) => s switch
    {
        ExternalPathwayStatus.Ok            => "OK",
        ExternalPathwayStatus.NotFound      => "NOT_FOUND",
        ExternalPathwayStatus.Unavailable   => "UNAVAILABLE",
        _                                   => "NOT_CONFIGURED",
    };
}
