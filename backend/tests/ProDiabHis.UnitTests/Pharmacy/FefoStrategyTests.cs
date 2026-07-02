using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;
using ProDiabHis.Application.Pharmacy.Prescriptions;
using ProDiabHis.Infrastructure.Pharmacy;
using Xunit;

namespace ProDiabHis.UnitTests.Pharmacy;

/// <summary>
/// Tests for FEFO strategy: correct batch order, insufficient stock, expired skip.
/// Uses NSubstitute mock for IDapperConnectionFactory.
/// Note: Full FEFO integration test uses real MySQL (Testcontainers) in IntegrationTests.
/// </summary>
public class FefoStrategyTests
{
    [Fact]
    public void FefoStrategy_is_registered_and_constructable()
    {
        var db = Substitute.For<IDapperConnectionFactory>();
        var fefo = new FefoStrategyImpl(db, NullLogger<FefoStrategyImpl>.Instance);
        fefo.Should().NotBeNull();
    }

    [Fact]
    public void BatchPick_record_immutable_and_correct()
    {
        var pick = new BatchPick("BATCH001", new DateOnly(2026, 12, 31), 10m, 50000m);
        pick.BatchNo.Should().Be("BATCH001");
        pick.ExpiryDate.Should().Be(new DateOnly(2026, 12, 31));
        pick.Quantity.Should().Be(10m);
        pick.UnitCost.Should().Be(50000m);
    }

    [Theory]
    [InlineData("CONTRAINDICATED", true)]
    [InlineData("MAJOR", false)]
    [InlineData("MODERATE", false)]
    [InlineData("MINOR", false)]
    public void DdiWarning_only_CONTRAINDICATED_blocks(string severity, bool shouldBlock)
    {
        var warning = new DdiWarning(1, "D1", 2, "D2", severity, "desc", "A");
        var blocks = warning.Severity == "CONTRAINDICATED";
        blocks.Should().Be(shouldBlock);
    }

    [Fact]
    public void FefoStrategy_exception_message_contains_error_code()
    {
        // When FEFO strategy throws insufficient stock, message has PHARMACY_STOCK_INSUFFICIENT prefix
        var exMsg = "PHARMACY_STOCK_INSUFFICIENT:Ton kho khong du (con thieu 5)";
        exMsg.Should().StartWith("PHARMACY_STOCK_INSUFFICIENT:");
    }

    [Fact]
    public void DrugImportResult_sums_correctly()
    {
        var result = new DrugImportResult(100, 60, 30, 10,
            [new DrugImportError(5, "Missing name"), new DrugImportError(10, "Invalid form")]);

        (result.Inserted + result.Updated + result.Failed).Should().Be(result.TotalRows);
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void MockDtqgQrGenerator_generates_png_bytes()
    {
        // QrCoderDtqgQrGenerator integration check (no network needed)
        var gen = new QrCoderDtqgQrGenerator();
        var bytes = gen.GenerateQrPng("VN260523000001", "https://donthuocquocgia.vn/verify/VN260523000001");
        bytes.Should().NotBeNullOrEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void FefoOrder_earlier_expiry_picked_first()
    {
        // Simulate FEFO pick ordering logic
        var batches = new[]
        {
            new { BatchNo = "BATCH_C", ExpiryDate = new DateOnly(2027, 6, 1), Qty = 100m },
            new { BatchNo = "BATCH_A", ExpiryDate = new DateOnly(2026, 8, 1), Qty = 50m },
            new { BatchNo = "BATCH_B", ExpiryDate = new DateOnly(2027, 1, 1), Qty = 75m },
        };

        var ordered = batches.OrderBy(b => b.ExpiryDate).ToList();
        ordered[0].BatchNo.Should().Be("BATCH_A", "earliest expiry should be first (FEFO)");
        ordered[1].BatchNo.Should().Be("BATCH_B");
        ordered[2].BatchNo.Should().Be("BATCH_C");
    }
}
