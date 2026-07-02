using Dapper;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Infrastructure.Persistence;
using System.Text.Json;

namespace ProDiabHis.Infrastructure.Billing;

public class CashierShiftServiceImpl : ICashierShiftService
{
    private readonly AppDbContext _db;
    private readonly IDapperConnectionFactory _dapper;

    public CashierShiftServiceImpl(AppDbContext db, IDapperConnectionFactory dapper)
    {
        _db = db;
        _dapper = dapper;
    }

    public async Task<CashierShift?> GetOpenShiftAsync(Guid userId, int tenantId, CancellationToken ct = default)
    {
        return await _db.CashierShifts
            .FirstOrDefaultAsync(s => s.CashierUserId == userId && s.TenantId == tenantId && s.Status == "OPEN", ct);
    }

    public async Task<CashierShift> CalculateShiftSummaryAsync(CashierShift shift, CancellationToken ct = default)
    {
        using var conn = _dapper.CreateConnection();

        var payments = await conn.QueryAsync<dynamic>(
            @"SELECT method, status, amount FROM diab_his_bil_payments
              WHERE cashier_shift_id = @shiftId AND tenant_id = @tenantId",
            new { shiftId = shift.Id.ToString(), tenantId = shift.TenantId });

        decimal totalCash = 0, totalCard = 0, totalTransfer = 0, totalQr = 0, totalOther = 0;
        decimal totalRefund = 0, totalVoid = 0;
        int count = 0;
        var breakdown = new Dictionary<string, (decimal Amount, int Count)>();

        foreach (var p in payments)
        {
            var method = (string)p.method;
            var status = (string)p.status;
            var amount = (decimal)p.amount;

            if (status == PaymentStatus.Void) { totalVoid += Math.Abs(amount); continue; }
            if (status == PaymentStatus.Refunded || amount < 0) { totalRefund += Math.Abs(amount); continue; }

            count++;
            switch (method)
            {
                case "CASH": totalCash += amount; break;
                case "VISA": case "MASTER": totalCard += amount; break;
                case "BANK_TRANSFER": totalTransfer += amount; break;
                case "QR_VIETQR": case "QR_MOMO": case "QR_VNPAY": totalQr += amount; break;
                default: totalOther += amount; break;
            }

            if (!breakdown.ContainsKey(method)) breakdown[method] = (0, 0);
            breakdown[method] = (breakdown[method].Amount + amount, breakdown[method].Count + 1);
        }

        shift.TotalCash = totalCash;
        shift.TotalCard = totalCard;
        shift.TotalTransfer = totalTransfer;
        shift.TotalQr = totalQr;
        shift.TotalOther = totalOther;
        shift.TotalRefund = totalRefund;
        shift.TotalVoid = totalVoid;
        shift.CountTransactions = count;
        shift.BreakdownJson = JsonSerializer.Serialize(
            breakdown.Select(kv => new { method = kv.Key, amount = kv.Value.Amount, count = kv.Value.Count }));

        return shift;
    }
}
