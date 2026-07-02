using FluentAssertions;
using NSubstitute;
using ProDiabHis.Api.Services;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ProDiabHis.UnitTests.Billing;

/// <summary>
/// Unit tests cho InvoicePdfService va ReceiptPdfService (QuestPDF).
/// Tests xac nhan: PDF bytes khac rong, khong throw, reprint flag hoat dong.
/// </summary>
public class InvoicePdfServiceTests
{
    private readonly IInvoicePdfService _svc = new InvoicePdfService();

    private static BillingResponse MakeBilling(string status = "FINALIZED") => new(
        Id: Guid.NewGuid(),
        TenantId: 1,
        EncounterId: Guid.NewGuid(),
        PatientId: Guid.NewGuid(),
        PatientSummary: new PatientSummaryDto("Nguyen Van A", DateOnly.Parse("1990-01-01"), "MALE", "0901234567", null),
        BillNo: "HD-202605-ABCDE",
        Items: new List<BillingItemDto>
        {
            new(Guid.NewGuid(), "SERVICE", null, "DV001", "Kham tong quat", 1, 150_000, 0, 0, 150_000, false, 0),
            new(Guid.NewGuid(), "DRUG",    null, "MED01", "Metformin 500mg", 30, 2_000, 0, 0, 60_000, false, 0)
        },
        Subtotal: 210_000,
        VatTotal: 0,
        DiscountAmount: 0,
        BhytAmount: 0,
        PatientPayable: 210_000,
        PaidAmount: 210_000,
        Balance: 0,
        Status: status,
        PaymentDueDate: null,
        Payer: "SELF",
        Note: null,
        CreatedAt: DateTime.UtcNow,
        CreatedBy: null,
        FinalizedAt: DateTime.UtcNow,
        VoidReason: null);

    [Fact]
    public async Task GenerateInvoicePdf_HappyPath_ReturnsPdfBytes()
    {
        var billing = MakeBilling();
        var options = new PrintBillingOptions("BAN GOC", false);

        var bytes = await _svc.GenerateInvoicePdfAsync(billing, options);

        bytes.Should().NotBeNullOrEmpty("PDF phai co noi dung");
        bytes.Length.Should().BeGreaterThan(1000, "PDF thuc phai nang hon 1KB");
        // PDF magic bytes: %PDF
        bytes[0].Should().Be(0x25); // '%'
        bytes[1].Should().Be(0x50); // 'P'
        bytes[2].Should().Be(0x44); // 'D'
        bytes[3].Should().Be(0x46); // 'F'
    }

    [Fact]
    public async Task GenerateInvoicePdf_Reprint_ReturnsDifferentContent_NoException()
    {
        var billing = MakeBilling();
        var optNormal = new PrintBillingOptions("BAN GOC", false);
        var optReprint = new PrintBillingOptions("BAN KHACH HANG", true);

        var normal = await _svc.GenerateInvoicePdfAsync(billing, optNormal);
        var reprint = await _svc.GenerateInvoicePdfAsync(billing, optReprint);

        normal.Should().NotBeNullOrEmpty();
        reprint.Should().NotBeNullOrEmpty();
        // Reprint co them [IN LAI] label nen bytes khac
        normal.SequenceEqual(reprint).Should().BeFalse(
            "PDF reprint phai co thay doi noi dung [IN LAI]");
    }

    [Fact]
    public async Task GenerateInvoicePdf_BillingWithBhyt_RendersCorrectly()
    {
        var billing = MakeBilling() with
        {
            BhytAmount = 100_000,
            PatientPayable = 110_000
        };

        var bytes = await _svc.GenerateInvoicePdfAsync(billing, new PrintBillingOptions());

        bytes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateInvoicePdf_EmptyItems_DoesNotThrow()
    {
        var billing = MakeBilling() with { Items = new List<BillingItemDto>() };

        var bytes = await _svc.GenerateInvoicePdfAsync(billing, new PrintBillingOptions());

        bytes.Should().NotBeNullOrEmpty("PDF rong item van phai render");
    }
}

public class ReceiptPdfServiceTests
{
    private readonly IReceiptPdfService _svc = new ReceiptPdfService();

    private static ReceiptData MakeReceipt() => new(
        ReceiptNo: "BL-260531-ABCDEF",
        TenantName: "Phong Kham Pro-Diab",
        TenantAddress: "123 Nguyen Hue, Q1, TP.HCM",
        TenantCskcbCode: "79001234",
        PatientCode: "BN001234",
        PatientName: "Nguyen Thi B",
        Phone: "0912345678",
        PaidAt: DateTime.UtcNow,
        Method: "CASH",
        Amount: 210_000,
        Reference: null,
        CashierName: "Thu ngan A",
        Items: new List<ReceiptLineItem>
        {
            new("Kham tong quat", 1, 150_000, 150_000),
            new("Metformin 500mg", 30, 2_000, 60_000)
        });

    [Fact]
    public async Task GenerateReceiptPdf_HappyPath_ReturnsK80Pdf()
    {
        var receipt = MakeReceipt();

        var bytes = await _svc.GenerateReceiptPdfAsync(receipt, false);

        bytes.Should().NotBeNullOrEmpty();
        bytes[0].Should().Be(0x25); // PDF magic '%'
        bytes.Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public async Task GenerateReceiptPdf_Reprint_AddsReprintBadge()
    {
        var receipt = MakeReceipt();

        var normal = await _svc.GenerateReceiptPdfAsync(receipt, false);
        var reprint = await _svc.GenerateReceiptPdfAsync(receipt, true);

        normal.Should().NotBeNullOrEmpty();
        reprint.Should().NotBeNullOrEmpty();
        normal.SequenceEqual(reprint).Should().BeFalse("Reprint phai khac ban thuong");
    }

    [Fact]
    public async Task GenerateReceiptPdf_NullOptionalFields_DoesNotThrow()
    {
        var receipt = MakeReceipt() with
        {
            TenantAddress = null,
            TenantCskcbCode = null,
            PatientCode = null,
            Phone = null,
            Reference = null,
            CashierName = null
        };

        var bytes = await _svc.GenerateReceiptPdfAsync(receipt);

        bytes.Should().NotBeNullOrEmpty();
    }
}
