using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities.Bhyt;
using ProDiabHis.Infrastructure.Bhyt;
using System.Text;
using Xunit;

namespace ProDiabHis.UnitTests.Bhyt;

public class BhytReconcileParserTests
{
    private readonly IFileStorage _fileStorage;
    private readonly BhytReconcileParserImpl _parser;

    public BhytReconcileParserTests()
    {
        _fileStorage = Substitute.For<IFileStorage>();
        _parser = new BhytReconcileParserImpl(_fileStorage, NullLogger<BhytReconcileParserImpl>.Instance);
    }

    private void SetupFileContent(string xml)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        _fileStorage.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Stream>(stream));
    }

    [Fact]
    public async Task Parse_approved_item_correctly()
    {
        // Arrange
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <KetQuaGiamDinh>
              <Items>
                <Item tableNo="1" maLienKet="CLINIC001-enc-001" requestAmount="500000" approvedAmount="500000" status="APPROVED"/>
              </Items>
            </KetQuaGiamDinh>
            """;
        SetupFileContent(xml);

        // Act
        var result = await _parser.ParseAsync("some/path.xml", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Status.Should().Be(BhytReconcileItemStatus.Approved);
        item.ApprovedAmount.Should().Be(500000m);
        item.RejectedAmount.Should().Be(0m);
        item.MaLienKet.Should().Be("CLINIC001-enc-001");
    }

    [Fact]
    public async Task Parse_rejected_item_with_reason()
    {
        // Arrange
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <KetQuaGiamDinh>
              <Items>
                <Item tableNo="2" maLienKet="CLINIC001-enc-002" requestAmount="200000" approvedAmount="0"
                      status="REJECTED" rejectionCode="RC01" rejectionReason="Khong du dieu kien"/>
              </Items>
            </KetQuaGiamDinh>
            """;
        SetupFileContent(xml);

        // Act
        var result = await _parser.ParseAsync("some/path.xml", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Status.Should().Be(BhytReconcileItemStatus.Rejected);
        item.ApprovedAmount.Should().Be(0m);
        item.RejectedAmount.Should().Be(200000m);
        item.RejectionCode.Should().Be("RC01");
        item.RejectionReason.Should().Be("Khong du dieu kien");
    }

    [Fact]
    public async Task Parse_adjusted_item_with_partial_approval()
    {
        // Arrange
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <KetQuaGiamDinh>
              <Items>
                <Item tableNo="1" maLienKet="CLINIC001-enc-003" requestAmount="300000" approvedAmount="200000"
                      status="ADJUSTED" rejectionCode="ADJ01" rejectionReason="Dieu chinh don gia"/>
              </Items>
            </KetQuaGiamDinh>
            """;
        SetupFileContent(xml);

        // Act
        var result = await _parser.ParseAsync("some/path.xml", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var item = result.Items[0];
        item.Status.Should().Be(BhytReconcileItemStatus.Adjusted);
        item.ApprovedAmount.Should().Be(200000m);
        item.RejectedAmount.Should().Be(100000m);
    }

    [Fact]
    public async Task Parse_multiple_items_mixed_statuses()
    {
        // Arrange
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <KetQuaGiamDinh>
              <Items>
                <Item tableNo="1" maLienKet="LK001" requestAmount="100000" approvedAmount="100000" status="APPROVED"/>
                <Item tableNo="2" maLienKet="LK002" requestAmount="50000" approvedAmount="0" status="REJECTED" rejectionCode="RC02" rejectionReason="Het han the"/>
                <Item tableNo="3" maLienKet="LK003" requestAmount="80000" approvedAmount="60000" status="ADJUSTED"/>
              </Items>
            </KetQuaGiamDinh>
            """;
        SetupFileContent(xml);

        // Act
        var result = await _parser.ParseAsync("some/path.xml", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Items.Should().HaveCount(3);
        result.Items.Count(i => i.Status == BhytReconcileItemStatus.Approved).Should().Be(1);
        result.Items.Count(i => i.Status == BhytReconcileItemStatus.Rejected).Should().Be(1);
        result.Items.Count(i => i.Status == BhytReconcileItemStatus.Adjusted).Should().Be(1);
    }

    [Fact]
    public async Task Parse_invalid_xml_returns_failure()
    {
        // Arrange
        SetupFileContent("NOT VALID XML <<<");

        // Act
        var result = await _parser.ParseAsync("some/path.xml", CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Items.Should().BeEmpty();
    }
}
