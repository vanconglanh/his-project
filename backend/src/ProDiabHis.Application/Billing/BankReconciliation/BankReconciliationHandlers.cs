using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Billing.BankReconciliation;

// ── Commands / Queries ───────────────────────────────────────────────────────

public record ImportBankStatementCommand(
    Stream FileStream, string FileName, string? ContentType,
    string? BankCode, DateOnly? StatementDate) : IRequest<Result<BankStatementResponse>>;

public record ListBankStatementsQuery(
    DateOnly? FromDate, DateOnly? ToDate, int Page, int PageSize)
    : IRequest<Result<PagedResult<BankStatementResponse>>>;

public record GetBankStatementLinesQuery(Guid StatementId) : IRequest<Result<BankStatementLinesResponse>>;

public record GetMatchCandidatesQuery(Guid LineId) : IRequest<Result<List<PaymentCandidateDto>>>;

public record ManualMatchLineCommand(Guid LineId, Guid PaymentId) : IRequest<Result<BankStatementLineResponse>>;

public record IgnoreLineCommand(Guid LineId) : IRequest<Result<BankStatementLineResponse>>;

public record UnmatchLineCommand(Guid LineId) : IRequest<Result<BankStatementLineResponse>>;

// Method BANK/the/QR duoc phep doi soat (loai CASH, OTHER)
internal static class BankReconcileConstants
{
    public static readonly string[] MatchableMethods =
        { "BANK_TRANSFER", "VISA", "MASTER", "QR_VIETQR", "QR_MOMO", "QR_VNPAY" };
}

// ── Import + auto-match ──────────────────────────────────────────────────────

public class ImportBankStatementHandler : IRequestHandler<ImportBankStatementCommand, Result<BankStatementResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly IBankStatementParser _parser;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IBranchProvider _branch;
    private readonly ILogger<ImportBankStatementHandler> _logger;

    public ImportBankStatementHandler(
        IDapperConnectionFactory db, IBankStatementParser parser, ITenantProvider tenant,
        ICurrentUser user, IBranchProvider branch, ILogger<ImportBankStatementHandler> logger)
    {
        _db = db; _parser = parser; _tenant = tenant; _user = user; _branch = branch; _logger = logger;
    }

    public async Task<Result<BankStatementResponse>> Handle(ImportBankStatementCommand cmd, CancellationToken ct)
    {
        List<BankStatementRawLine> rawLines;
        try
        {
            rawLines = await _parser.ParseAsync(cmd.FileStream, cmd.FileName, cmd.ContentType, ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bank statement parse error for file {FileName}", cmd.FileName);
            var msg = ex.Message.Contains(':') ? ex.Message[(ex.Message.IndexOf(':') + 1)..] : ex.Message;
            return Result<BankStatementResponse>.Failure("BANK_STATEMENT_INVALID_FORMAT",
                string.IsNullOrWhiteSpace(msg) ? "Định dạng file sao kê không hợp lệ." : msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bank statement parse unexpected error for file {FileName}", cmd.FileName);
            return Result<BankStatementResponse>.Failure("BANK_STATEMENT_INVALID_FORMAT", "Định dạng file sao kê không hợp lệ.");
        }

        if (rawLines.Count == 0)
            return Result<BankStatementResponse>.Failure("BANK_STATEMENT_INVALID_FORMAT", "File sao kê không có dòng dữ liệu nào.");

        var tenantId = _tenant.TenantId;
        var userId = _user.UserId;
        var branchId = _branch.BranchId > 0 ? (int?)_branch.BranchId : null;
        var statementId = Guid.NewGuid();

        using var conn = _db.CreateConnection();

        // Nap danh sach payment ung vien cua tenant de auto-match
        var candidates = (await conn.QueryAsync<PaymentCandidateRow>(
            @"SELECT id, reference, method, amount, paid_at, billing_id
              FROM diab_his_bil_payments
              WHERE tenant_id = @tenantId AND status = 'COMPLETED'
                AND method IN @methods",
            new { tenantId, methods = BankReconcileConstants.MatchableMethods })).ToList();

        var used = new HashSet<string>();
        int matchedCount = 0;
        var lineRows = new List<object>();

        foreach (var raw in rawLines)
        {
            string matchStatus = "UNMATCHED";
            string? matchedPaymentId = null;

            var sameAmountAndDate = candidates
                .Where(p => !used.Contains(p.id)
                    && p.amount == raw.Amount
                    && raw.TransactionDate.HasValue && p.paid_at.HasValue
                    && Math.Abs((DateOnly.FromDateTime(p.paid_at.Value).DayNumber - raw.TransactionDate.Value.DayNumber)) <= 1)
                .ToList();

            PaymentCandidateRow? chosen = null;

            if (!string.IsNullOrWhiteSpace(raw.ReferenceNo))
            {
                var refTrim = raw.ReferenceNo.Trim();
                var refMatch = sameAmountAndDate
                    .Where(p => !string.IsNullOrWhiteSpace(p.reference) && p.reference!.Trim() == refTrim)
                    .ToList();
                if (refMatch.Count == 1) chosen = refMatch[0];
            }

            if (chosen == null && sameAmountAndDate.Count == 1)
                chosen = sameAmountAndDate[0];

            if (chosen != null)
            {
                matchStatus = "MATCHED";
                matchedPaymentId = chosen.id;
                used.Add(chosen.id);
                matchedCount++;
            }

            var lineId = Guid.NewGuid().ToString();
            lineRows.Add(new
            {
                id = lineId,
                tenantId,
                statementId = statementId.ToString(),
                transactionDate = raw.TransactionDate?.ToString("yyyy-MM-dd"),
                amount = raw.Amount,
                referenceNo = raw.ReferenceNo,
                description = raw.Description,
                matchedPaymentId,
                matchStatus,
                matchedAt = matchStatus == "MATCHED" ? DateTime.UtcNow : (DateTime?)null
            });
        }

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_bil_bank_statements
              (id, tenant_id, branch_id, file_name, bank_code, statement_date, total_lines, matched_lines,
               uploaded_by, uploaded_at, created_at, created_by, updated_at)
              VALUES
              (@id, @tenantId, @branchId, @fileName, @bankCode, @statementDate, @totalLines, @matchedLines,
               @uploadedBy, NOW(3), NOW(3), @uploadedBy, NOW(3))",
            new
            {
                id = statementId.ToString(),
                tenantId,
                branchId,
                fileName = cmd.FileName,
                bankCode = cmd.BankCode,
                statementDate = cmd.StatementDate?.ToString("yyyy-MM-dd"),
                totalLines = rawLines.Count,
                matchedLines = matchedCount,
                uploadedBy = userId?.ToString()
            });

        foreach (var row in lineRows)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_bil_bank_statement_lines
                  (id, tenant_id, statement_id, transaction_date, amount, reference_no, description,
                   matched_payment_id, match_status, matched_at, created_at, updated_at)
                  VALUES
                  (@id, @tenantId, @statementId, @transactionDate, @amount, @referenceNo, @description,
                   @matchedPaymentId, @matchStatus, @matchedAt, NOW(3), NOW(3))",
                row);
        }

        _logger.LogInformation("Import bank statement {File} tenant {TenantId}: {Total} dong, {Matched} khop tu dong",
            cmd.FileName, tenantId, rawLines.Count, matchedCount);

        return Result<BankStatementResponse>.Success(new BankStatementResponse(
            statementId, cmd.FileName, cmd.BankCode, cmd.StatementDate,
            rawLines.Count, matchedCount, rawLines.Count - matchedCount, DateTime.UtcNow, null));
    }

    private record PaymentCandidateRow(string id, string? reference, string method, decimal amount, DateTime? paid_at, string billing_id);
}

// ── List statements ───────────────────────────────────────────────────────

public class ListBankStatementsHandler : IRequestHandler<ListBankStatementsQuery, Result<PagedResult<BankStatementResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListBankStatementsHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PagedResult<BankStatementResponse>>> Handle(ListBankStatementsQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;
        var where = "WHERE s.tenant_id = @tenantId AND s.deleted_at IS NULL";
        var p = new DynamicParameters();
        p.Add("tenantId", tenantId);

        if (query.FromDate.HasValue) { where += " AND s.statement_date >= @from"; p.Add("from", query.FromDate.Value.ToString("yyyy-MM-dd")); }
        if (query.ToDate.HasValue) { where += " AND s.statement_date <= @to"; p.Add("to", query.ToDate.Value.ToString("yyyy-MM-dd")); }

        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_bil_bank_statements s {where}", p);

        var offset = (query.Page - 1) * query.PageSize;
        p.Add("limit", query.PageSize); p.Add("offset", offset);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT s.id, s.file_name, s.bank_code, s.statement_date, s.total_lines, s.matched_lines,
                      s.uploaded_at, u.full_name AS uploaded_by_name
               FROM diab_his_bil_bank_statements s
               LEFT JOIN diab_his_sec_users u ON u.id = s.uploaded_by
               {where}
               ORDER BY s.uploaded_at DESC
               LIMIT @limit OFFSET @offset", p);

        var items = rows.Select(r => new BankStatementResponse(
            Guid.Parse((string)r.id),
            (string)r.file_name,
            (string?)r.bank_code,
            r.statement_date == null ? null : DateOnly.FromDateTime((DateTime)r.statement_date),
            (int)r.total_lines,
            (int)r.matched_lines,
            (int)r.total_lines - (int)r.matched_lines,
            (DateTime)r.uploaded_at,
            (string?)r.uploaded_by_name)).ToList();

        return Result<PagedResult<BankStatementResponse>>.Success(
            new PagedResult<BankStatementResponse>(items, query.Page, query.PageSize, total));
    }
}

// ── Get statement lines ───────────────────────────────────────────────────

public class GetBankStatementLinesHandler : IRequestHandler<GetBankStatementLinesQuery, Result<BankStatementLinesResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetBankStatementLinesHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<BankStatementLinesResponse>> Handle(GetBankStatementLinesQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var stmt = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT s.id, s.file_name, s.bank_code, s.statement_date, s.total_lines, s.matched_lines,
                     s.uploaded_at, u.full_name AS uploaded_by_name
              FROM diab_his_bil_bank_statements s
              LEFT JOIN diab_his_sec_users u ON u.id = s.uploaded_by
              WHERE s.id = @id AND s.tenant_id = @tenantId AND s.deleted_at IS NULL",
            new { id = query.StatementId.ToString(), tenantId });

        if (stmt == null)
            return Result<BankStatementLinesResponse>.Failure("BANK_STATEMENT_NOT_FOUND", "Không tìm thấy sao kê.");

        var statementDto = new BankStatementResponse(
            Guid.Parse((string)stmt.id), (string)stmt.file_name, (string?)stmt.bank_code,
            stmt.statement_date == null ? null : DateOnly.FromDateTime((DateTime)stmt.statement_date),
            (int)stmt.total_lines, (int)stmt.matched_lines, (int)stmt.total_lines - (int)stmt.matched_lines,
            (DateTime)stmt.uploaded_at, (string?)stmt.uploaded_by_name);

        var lineRows = await conn.QueryAsync<dynamic>(
            @"SELECT l.id, l.transaction_date, l.amount, l.reference_no, l.description,
                     l.match_status, l.matched_payment_id,
                     p.id AS p_id, p.reference AS p_reference, p.method AS p_method,
                     p.amount AS p_amount, p.paid_at AS p_paid_at, p.billing_id AS p_billing_id
              FROM diab_his_bil_bank_statement_lines l
              LEFT JOIN diab_his_bil_payments p ON p.id = l.matched_payment_id AND p.tenant_id = l.tenant_id
              WHERE l.tenant_id = @tenantId AND l.statement_id = @statementId
              ORDER BY l.transaction_date, l.created_at",
            new { tenantId, statementId = query.StatementId.ToString() });

        var lines = lineRows.Select(MapLine).ToList();

        return Result<BankStatementLinesResponse>.Success(new BankStatementLinesResponse(statementDto, lines));
    }

    internal static BankStatementLineResponse MapLine(dynamic r)
    {
        MatchedPaymentDto? matchedPayment = null;
        if (r.p_id != null)
        {
            matchedPayment = new MatchedPaymentDto(
                Guid.Parse((string)r.p_id), (string?)r.p_reference, (string)r.p_method,
                (decimal)r.p_amount, r.p_paid_at == null ? null : (DateTime?)r.p_paid_at,
                Guid.Parse((string)r.p_billing_id));
        }

        return new BankStatementLineResponse(
            Guid.Parse((string)r.id),
            r.transaction_date == null ? null : DateOnly.FromDateTime((DateTime)r.transaction_date),
            (decimal)r.amount,
            (string?)r.reference_no,
            (string?)r.description,
            (string)r.match_status,
            r.matched_payment_id == null ? null : Guid.Parse((string)r.matched_payment_id),
            matchedPayment);
    }
}

// ── Get manual match candidates ────────────────────────────────────────────

public class GetMatchCandidatesHandler : IRequestHandler<GetMatchCandidatesQuery, Result<List<PaymentCandidateDto>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetMatchCandidatesHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<List<PaymentCandidateDto>>> Handle(GetMatchCandidatesQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var line = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, amount, transaction_date FROM diab_his_bil_bank_statement_lines WHERE id = @id AND tenant_id = @tenantId",
            new { id = query.LineId.ToString(), tenantId });

        if (line == null)
            return Result<List<PaymentCandidateDto>>.Failure("BANK_STATEMENT_LINE_NOT_FOUND", "Không tìm thấy dòng sao kê.");

        decimal lineAmount = (decimal)line.amount;
        DateTime? lineDate = line.transaction_date == null ? null : (DateTime)line.transaction_date;

        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT p.id, p.reference, p.method, p.amount, p.paid_at, p.billing_id
              FROM diab_his_bil_payments p
              WHERE p.tenant_id = @tenantId AND p.status = 'COMPLETED' AND p.method IN @methods
                AND NOT EXISTS (
                    SELECT 1 FROM diab_his_bil_bank_statement_lines l2
                    WHERE l2.matched_payment_id = p.id AND l2.tenant_id = @tenantId
                )",
            new { tenantId, methods = BankReconcileConstants.MatchableMethods });

        var candidates = rows.Select(r => new
        {
            dto = new PaymentCandidateDto(
                Guid.Parse((string)r.id), (string?)r.reference, (string)r.method,
                (decimal)r.amount, r.paid_at == null ? null : (DateTime?)r.paid_at, Guid.Parse((string)r.billing_id)),
            amountDiff = Math.Abs((decimal)r.amount - lineAmount),
            dateDiff = (r.paid_at == null || !lineDate.HasValue)
                ? double.MaxValue
                : Math.Abs(((DateTime)r.paid_at - lineDate.GetValueOrDefault()).TotalDays)
        })
        .OrderBy(x => x.amountDiff)
        .ThenBy(x => x.dateDiff)
        .Select(x => x.dto)
        .ToList();

        return Result<List<PaymentCandidateDto>>.Success(candidates);
    }
}

// ── Manual match ─────────────────────────────────────────────────────────

public class ManualMatchLineHandler : IRequestHandler<ManualMatchLineCommand, Result<BankStatementLineResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public ManualMatchLineHandler(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Result<BankStatementLineResponse>> Handle(ManualMatchLineCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var line = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, statement_id, match_status FROM diab_his_bil_bank_statement_lines WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.LineId.ToString(), tenantId });
        if (line == null)
            return Result<BankStatementLineResponse>.Failure("BANK_STATEMENT_LINE_NOT_FOUND", "Không tìm thấy dòng sao kê.");

        var payment = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_bil_payments WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.PaymentId.ToString(), tenantId });
        if (payment == null)
            return Result<BankStatementLineResponse>.Failure("PAYMENT_NOT_FOUND", "Không tìm thấy khoản thu.");

        var alreadyMatched = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM diab_his_bil_bank_statement_lines
              WHERE tenant_id = @tenantId AND matched_payment_id = @paymentId AND id <> @lineId",
            new { tenantId, paymentId = cmd.PaymentId.ToString(), lineId = cmd.LineId.ToString() });
        if (alreadyMatched > 0)
            return Result<BankStatementLineResponse>.Failure("PAYMENT_ALREADY_MATCHED", "Khoản thu này đã được khớp với dòng khác.");

        bool wasMatched = (string)line.match_status is "MATCHED" or "MANUAL_MATCHED";

        await conn.ExecuteAsync(
            @"UPDATE diab_his_bil_bank_statement_lines
              SET matched_payment_id = @paymentId, match_status = 'MANUAL_MATCHED',
                  matched_at = NOW(3), matched_by = @matchedBy, updated_at = NOW(3)
              WHERE id = @lineId AND tenant_id = @tenantId",
            new { paymentId = cmd.PaymentId.ToString(), matchedBy = _user.UserId?.ToString(), lineId = cmd.LineId.ToString(), tenantId });

        if (!wasMatched)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_bil_bank_statements SET matched_lines = matched_lines + 1, updated_at = NOW(3) WHERE id = @statementId AND tenant_id = @tenantId",
                new { statementId = (string)line.statement_id, tenantId });
        }

        return await ReloadLine(conn, cmd.LineId, tenantId);
    }

    internal static async Task<Result<BankStatementLineResponse>> ReloadLine(System.Data.IDbConnection conn, Guid lineId, int tenantId)
    {
        var r = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT l.id, l.transaction_date, l.amount, l.reference_no, l.description,
                     l.match_status, l.matched_payment_id,
                     p.id AS p_id, p.reference AS p_reference, p.method AS p_method,
                     p.amount AS p_amount, p.paid_at AS p_paid_at, p.billing_id AS p_billing_id
              FROM diab_his_bil_bank_statement_lines l
              LEFT JOIN diab_his_bil_payments p ON p.id = l.matched_payment_id AND p.tenant_id = l.tenant_id
              WHERE l.id = @id AND l.tenant_id = @tenantId",
            new { id = lineId.ToString(), tenantId });
        if (r == null) return Result<BankStatementLineResponse>.Failure("BANK_STATEMENT_LINE_NOT_FOUND", "Không tìm thấy dòng sao kê.");
        return Result<BankStatementLineResponse>.Success(GetBankStatementLinesHandler.MapLine(r));
    }
}

// ── Ignore ────────────────────────────────────────────────────────────────

public class IgnoreLineHandler : IRequestHandler<IgnoreLineCommand, Result<BankStatementLineResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public IgnoreLineHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<BankStatementLineResponse>> Handle(IgnoreLineCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var line = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, statement_id, match_status FROM diab_his_bil_bank_statement_lines WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.LineId.ToString(), tenantId });
        if (line == null)
            return Result<BankStatementLineResponse>.Failure("BANK_STATEMENT_LINE_NOT_FOUND", "Không tìm thấy dòng sao kê.");

        bool wasMatched = (string)line.match_status is "MATCHED" or "MANUAL_MATCHED";

        await conn.ExecuteAsync(
            @"UPDATE diab_his_bil_bank_statement_lines
              SET match_status = 'IGNORED', matched_payment_id = NULL, matched_at = NULL, matched_by = NULL, updated_at = NOW(3)
              WHERE id = @lineId AND tenant_id = @tenantId",
            new { lineId = cmd.LineId.ToString(), tenantId });

        if (wasMatched)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_bil_bank_statements SET matched_lines = matched_lines - 1, updated_at = NOW(3) WHERE id = @statementId AND tenant_id = @tenantId",
                new { statementId = (string)line.statement_id, tenantId });
        }

        return await ManualMatchLineHandler.ReloadLine(conn, cmd.LineId, tenantId);
    }
}

// ── Unmatch ───────────────────────────────────────────────────────────────

public class UnmatchLineHandler : IRequestHandler<UnmatchLineCommand, Result<BankStatementLineResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public UnmatchLineHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<BankStatementLineResponse>> Handle(UnmatchLineCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var line = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, statement_id, match_status FROM diab_his_bil_bank_statement_lines WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.LineId.ToString(), tenantId });
        if (line == null)
            return Result<BankStatementLineResponse>.Failure("BANK_STATEMENT_LINE_NOT_FOUND", "Không tìm thấy dòng sao kê.");

        bool wasMatched = (string)line.match_status is "MATCHED" or "MANUAL_MATCHED";

        await conn.ExecuteAsync(
            @"UPDATE diab_his_bil_bank_statement_lines
              SET match_status = 'UNMATCHED', matched_payment_id = NULL, matched_at = NULL, matched_by = NULL, updated_at = NOW(3)
              WHERE id = @lineId AND tenant_id = @tenantId",
            new { lineId = cmd.LineId.ToString(), tenantId });

        if (wasMatched)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_bil_bank_statements SET matched_lines = matched_lines - 1, updated_at = NOW(3) WHERE id = @statementId AND tenant_id = @tenantId",
                new { statementId = (string)line.statement_id, tenantId });
        }

        return await ManualMatchLineHandler.ReloadLine(conn, cmd.LineId, tenantId);
    }
}
