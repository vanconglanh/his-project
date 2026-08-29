using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Interfaces;

namespace ProDiabHis.Infrastructure.Services;

/// <summary>
/// Trien khai IPackageEntitlementService (FR-1204). Dung Dapper + ADO transaction
/// truc tiep (khong qua EF) de thuc hien SELECT ... FOR UPDATE pessimistic lock
/// theo dung 4 lop phong thu trong docs/erd/goi-dich-vu-dinh-muc.md muc 6.2:
///   L1 SELECT...FOR UPDATE, L2 UPDATE co dieu kien + version, L3 CHECK o DB,
///   L4 UNIQUE idempotency_key.
/// </summary>
public class PackageEntitlementService : IPackageEntitlementService
{
    private readonly IDapperConnectionFactory _dbFactory;
    private readonly ITenantProvider _tenant;
    private readonly ILogger<PackageEntitlementService> _logger;

    public PackageEntitlementService(IDapperConnectionFactory dbFactory, ITenantProvider tenant, ILogger<PackageEntitlementService> logger)
    {
        _dbFactory = dbFactory;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<PackageCoverageQuote> QuoteAsync(PackageCoverageRequest request, CancellationToken ct)
    {
        using var conn = _dbFactory.CreateConnection();
        conn.Open();
        var tenantId = _tenant.TenantId;
        var lines = new List<PackageCoverageLineResult>();
        var warnings = new List<string>();

        foreach (var line in request.Lines)
        {
            var candidates = await FindCandidateBalancesAsync(conn, null, tenantId, request.PatientId, line.ItemType, line.ItemRefId, ct);
            var remainingToCover = line.RequestedQuantity;
            decimal covered = 0, coveredAmount = 0;
            Guid? subscriptionId = null, balanceId = null;
            foreach (var b in candidates)
            {
                if (remainingToCover <= 0) break;
                var take = Math.Min(remainingToCover, (decimal)b.remaining_quantity);
                if (take <= 0) continue;
                covered += take;
                coveredAmount += take * (decimal)b.unit_price_snapshot;
                remainingToCover -= take;
                subscriptionId ??= Guid.Parse((string)b.subscription_id);
                balanceId ??= Guid.Parse((string)b.id);
                if ((bool)b.has_debt) warnings.Add("PACKAGE_HAS_OUTSTANDING_DEBT");
            }
            lines.Add(new PackageCoverageLineResult(line.ItemType, line.ItemRefId, line.RequestedQuantity,
                covered, line.RequestedQuantity - covered, coveredAmount, subscriptionId, balanceId, null));
        }

        return new PackageCoverageQuote(lines, warnings.Distinct().ToList());
    }

    public async Task<PackageCoverageQuote> ConsumeAsync(PackageCoverageRequest request, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        using var conn = _dbFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var lines = new List<PackageCoverageLineResult>();
            var warnings = new List<string>();

            foreach (var line in request.Lines)
            {
                var sourceItemKey = line.SourceItemId?.ToString() ?? request.SourceId.ToString();

                // L1: khoa mot lo cac balance ung vien theo thu tu id ASC (chong deadlock - muc 6.3)
                var candidates = await FindCandidateBalancesAsync(conn, tx, tenantId, request.PatientId, line.ItemType, line.ItemRefId, ct);

                var remainingToCover = line.RequestedQuantity;
                decimal totalCovered = 0, totalCoveredAmount = 0;
                Guid? lastSubscriptionId = null, lastBalanceId = null, lastUsageLogId = null;

                foreach (var b in candidates)
                {
                    if (remainingToCover <= 0) break;

                    var balanceId = (string)b.id;
                    var subscriptionId = (string)b.subscription_id;
                    var remaining = (decimal)b.remaining_quantity;
                    var version = (int)b.version;
                    var unitPrice = (decimal)b.unit_price_snapshot;

                    var take = Math.Min(remainingToCover, remaining);
                    if (take <= 0) continue;

                    // Idempotency key rieng cho tung balance vi 1 line co the tran qua nhieu subscription
                    var idemKey = $"{request.SourceType}:{sourceItemKey}:{balanceId}";

                    var existedLog = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        @"SELECT id, covered_quantity, excess_quantity, covered_amount, subscription_id, balance_id
                          FROM diab_his_pkg_usage_logs
                          WHERE tenant_id=@tenantId AND idempotency_key=@idemKey AND action='DEDUCT' LIMIT 1",
                        new { tenantId, idemKey }, tx);

                    if (existedLog != null)
                    {
                        // Da tru truoc do (retry) - khong tru lai, cong don ket qua cu
                        totalCovered += (decimal)existedLog.covered_quantity;
                        totalCoveredAmount += (decimal)existedLog.covered_amount;
                        remainingToCover -= (decimal)existedLog.covered_quantity;
                        lastSubscriptionId = Guid.Parse((string)existedLog.subscription_id);
                        lastBalanceId = Guid.Parse((string)existedLog.balance_id);
                        lastUsageLogId = Guid.Parse((string)existedLog.id);
                        continue;
                    }

                    // L2: UPDATE co dieu kien version + so du - lop phong thu thu 2
                    var affected = await conn.ExecuteAsync(
                        @"UPDATE diab_his_pkg_entitlement_balances
                          SET used_quantity = used_quantity + @take, version = version + 1, last_used_at = UTC_TIMESTAMP(3)
                          WHERE id=@balanceId AND tenant_id=@tenantId AND version=@version
                            AND total_quantity - used_quantity >= @take",
                        new { take, balanceId, tenantId, version }, tx);

                    if (affected != 1)
                    {
                        _logger.LogWarning("PackageEntitlementService: balance conflict {BalanceId}", balanceId);
                        throw new PackageBalanceConflictException(balanceId);
                    }

                    var coveredAmount = take * unitPrice;
                    var usageLogId = Guid.NewGuid().ToString();
                    await conn.ExecuteAsync(
                        @"INSERT INTO diab_his_pkg_usage_logs
                          (id, tenant_id, branch_id, subscription_id, balance_id, patient_id, source_type, source_id,
                           source_item_id, requested_quantity, covered_quantity, excess_quantity, covered_amount,
                           action, idempotency_key, used_at, performed_by, created_at, created_by)
                          VALUES (@usageLogId, @tenantId, @branchId, @subscriptionId, @balanceId, @patientId, @sourceType,
                           @sourceId, @sourceItemId, @requested, @covered, 0, @coveredAmount, 'DEDUCT', @idemKey,
                           UTC_TIMESTAMP(3), @performedBy, UTC_TIMESTAMP(), @performedBy)",
                        new
                        {
                            usageLogId,
                            tenantId,
                            branchId = request.BranchId,
                            subscriptionId,
                            balanceId,
                            patientId = request.PatientId.ToString(),
                            sourceType = request.SourceType,
                            sourceId = request.SourceId.ToString(),
                            sourceItemId = line.SourceItemId?.ToString(),
                            requested = line.RequestedQuantity,
                            covered = take,
                            coveredAmount,
                            idemKey,
                            performedBy = request.PerformedBy?.ToString()
                        }, tx);

                    totalCovered += take;
                    totalCoveredAmount += coveredAmount;
                    remainingToCover -= take;
                    lastSubscriptionId = Guid.Parse(subscriptionId);
                    lastBalanceId = Guid.Parse(balanceId);
                    lastUsageLogId = Guid.Parse(usageLogId);

                    if ((bool)b.has_debt) warnings.Add("PACKAGE_HAS_OUTSTANDING_DEBT");

                    // RULE-S3: subscription het dinh muc -> exhausted
                    var stillHasRemaining = await conn.ExecuteScalarAsync<int>(
                        @"SELECT COUNT(*) FROM diab_his_pkg_entitlement_balances
                          WHERE subscription_id=@subscriptionId AND remaining_quantity > 0", new { subscriptionId }, tx);
                    if (stillHasRemaining == 0)
                    {
                        await conn.ExecuteAsync(
                            @"UPDATE diab_his_pkg_subscriptions SET status='exhausted'
                              WHERE id=@subscriptionId AND status='active'", new { subscriptionId }, tx);
                    }
                }

                lines.Add(new PackageCoverageLineResult(line.ItemType, line.ItemRefId, line.RequestedQuantity,
                    totalCovered, line.RequestedQuantity - totalCovered, totalCoveredAmount,
                    lastSubscriptionId, lastBalanceId, lastUsageLogId));
            }

            tx.Commit();
            return new PackageCoverageQuote(lines, warnings.Distinct().ToList());
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<PackageReverseResult> ReverseAsync(string sourceType, Guid sourceId, string reason, Guid? performedBy, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        using var conn = _dbFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var logs = (await conn.QueryAsync<dynamic>(
                @"SELECT l.id, l.subscription_id, l.balance_id, l.covered_quantity, l.source_item_id
                  FROM diab_his_pkg_usage_logs l
                  WHERE l.tenant_id=@tenantId AND l.source_type=@sourceType AND l.source_id=@sourceId
                    AND l.action='DEDUCT'
                    AND NOT EXISTS (SELECT 1 FROM diab_his_pkg_usage_logs r
                                    WHERE r.reversal_of_id = l.id AND r.action='REVERSE')
                  ORDER BY l.balance_id ASC",
                new { tenantId, sourceType, sourceId = sourceId.ToString() }, tx)).ToList();

            // Q6 - chan hoan dinh muc neu nguon goc da "chot" o he thong khac (hoa don da PAID
            // hoac thuoc da phat DISPENSED). Kiem tra TRUOC khi tru bat ky dong nao (all-or-nothing)
            // de tranh hoan mot phan roi bao loi giua chung.
            foreach (var log in logs)
            {
                var logId = (string)log.id;

                var paidBillingCount = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM diab_his_bil_billing_items bi
                      JOIN diab_his_bil_billing b ON b.id = bi.billing_id
                      WHERE bi.covered_by_usage_log_id = @logId AND b.status = 'PAID'",
                    new { logId }, tx);
                if (paidBillingCount > 0)
                    throw new PackageReverseNotAllowedException(
                        "Không thể hoàn định mức vì hoá đơn liên quan đã thanh toán");

                if (sourceType == "PRESCRIPTION" && log.source_item_id != null)
                {
                    var dispensedCount = await conn.ExecuteScalarAsync<int>(
                        @"SELECT COUNT(*) FROM diab_his_pha_dispense_items di
                          JOIN diab_his_pha_dispense_records dr ON dr.id = di.dispense_record_id
                          WHERE di.prescription_item_id = @itemId AND di.deleted_at IS NULL
                            AND dr.status = 'DISPENSED'",
                        new { itemId = (string)log.source_item_id }, tx);
                    if (dispensedCount > 0)
                        throw new PackageReverseNotAllowedException(
                            "Không thể hoàn định mức vì thuốc liên quan đã được cấp phát");
                }
            }

            var warnings = new List<string>();
            foreach (var log in logs)
            {
                var balanceId = (string)log.balance_id;
                var subscriptionId = (string)log.subscription_id;
                var qty = (decimal)log.covered_quantity;

                var balRow = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT version FROM diab_his_pkg_entitlement_balances WHERE id=@balanceId FOR UPDATE",
                    new { balanceId }, tx);
                if (balRow == null) continue;
                var version = (int)balRow.version;

                var affected = await conn.ExecuteAsync(
                    @"UPDATE diab_his_pkg_entitlement_balances
                      SET used_quantity = used_quantity - @qty, version = version + 1
                      WHERE id=@balanceId AND version=@version AND used_quantity >= @qty",
                    new { qty, balanceId, version }, tx);
                if (affected != 1)
                {
                    warnings.Add("PACKAGE_REVERSE_CONFLICT");
                    continue;
                }

                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_pkg_usage_logs
                      (id, tenant_id, branch_id, subscription_id, balance_id, patient_id, source_type, source_id,
                       requested_quantity, covered_quantity, excess_quantity, covered_amount,
                       action, reversal_of_id, idempotency_key, used_at, performed_by, created_at, created_by)
                      SELECT UUID(), tenant_id, branch_id, subscription_id, balance_id, patient_id, source_type, source_id,
                       covered_quantity, covered_quantity, 0, 0, 'REVERSE', @logId,
                       CONCAT('REVERSE:', idempotency_key), UTC_TIMESTAMP(3), @performedBy, UTC_TIMESTAMP(), @performedBy
                      FROM diab_his_pkg_usage_logs WHERE id=@logId",
                    new { logId = (string)log.id, performedBy = performedBy?.ToString() }, tx);

                // Neu subscription dang exhausted va con han -> mo lai active
                await conn.ExecuteAsync(
                    @"UPDATE diab_his_pkg_subscriptions
                      SET status='active'
                      WHERE id=@subscriptionId AND status='exhausted' AND expiry_date >= CURDATE()",
                    new { subscriptionId }, tx);
            }

            tx.Commit();
            return new PackageReverseResult(logs.Count, warnings);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<PackagePatientSummary> GetPatientSummaryAsync(Guid patientId, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        using var conn = _dbFactory.CreateConnection();

        var subs = await conn.QueryAsync<dynamic>(
            @"SELECT id, subscription_no, package_name_snapshot, status, payment_status, expiry_date, amount_due
              FROM diab_his_pkg_subscriptions
              WHERE tenant_id=@tenantId AND patient_id=@patientId AND deleted_at IS NULL
                AND status IN ('active','suspended','exhausted','pending_payment','expired')
              ORDER BY expiry_date ASC",
            new { tenantId, patientId = patientId.ToString() });

        var result = new List<PackageSubscriptionSummary>();
        decimal totalDebt = 0;
        var hasExpiringSoon = false;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var s in subs)
        {
            var subId = (string)s.id;
            var balances = await conn.QueryAsync<dynamic>(
                @"SELECT item_type, item_code, item_name, unit, total_quantity, used_quantity, remaining_quantity
                  FROM diab_his_pkg_entitlement_balances WHERE subscription_id=@subId ORDER BY item_type, item_name",
                new { subId });

            var balList = balances.Select(b =>
            {
                var total = (decimal)b.total_quantity;
                var remaining = (decimal)b.remaining_quantity;
                var isLow = total > 0 && remaining / total <= 0.15m;
                return new PackageBalanceSummary(
                    (string)b.item_type, (string)b.item_code, (string)b.item_name, (string)b.unit,
                    total, (decimal)b.used_quantity, remaining,
                    $"còn {remaining:0.###}/{total:0.###}", isLow);
            }).ToList();

            var expiry = DateOnly.FromDateTime((DateTime)s.expiry_date);
            var daysToExpiry = expiry.DayNumber - today.DayNumber;
            if (daysToExpiry is >= 0 and <= 7 && (string)s.status == "active") hasExpiringSoon = true;

            var amountDue = (decimal)s.amount_due;
            totalDebt += amountDue;

            result.Add(new PackageSubscriptionSummary(
                Guid.Parse(subId), (string)s.subscription_no, (string)s.package_name_snapshot,
                (string)s.status, (string)s.payment_status, expiry, daysToExpiry, amountDue, balList));
        }

        return new PackagePatientSummary(totalDebt, hasExpiringSoon, result);
    }

    /// <summary>
    /// FIFO theo expiry_date (muc 6.4): tim cac balance con dinh muc cua benh nhan, khoa theo
    /// thu tu id ASC de tranh deadlock (muc 6.3). tx = null -> chi doc (Quote), khong khoa.
    /// </summary>
    private static async Task<List<dynamic>> FindCandidateBalancesAsync(
        IDbConnection conn, IDbTransaction? tx, int tenantId, Guid patientId,
        PackageItemType itemType, Guid itemRefId, CancellationToken ct)
    {
        var forUpdate = tx != null ? "FOR UPDATE" : "";
        var sql = $@"
            SELECT b.id, b.subscription_id, b.remaining_quantity, b.version, b.unit_price_snapshot,
                   (s.amount_due > 0) AS has_debt
            FROM diab_his_pkg_entitlement_balances b
            JOIN diab_his_pkg_subscriptions s ON s.id = b.subscription_id
            WHERE s.tenant_id=@tenantId AND s.patient_id=@patientId AND s.status='active'
              AND s.expiry_date >= CURDATE()
              AND b.item_type=@itemType AND b.item_ref_id=@itemRefId AND b.remaining_quantity > 0
            ORDER BY s.expiry_date ASC, s.purchase_date ASC, b.id ASC
            {forUpdate}";

        var rows = await conn.QueryAsync<dynamic>(sql,
            new { tenantId, patientId = patientId.ToString(), itemType = itemType.ToString(), itemRefId = itemRefId.ToString() }, tx);
        return rows.ToList();
    }
}

