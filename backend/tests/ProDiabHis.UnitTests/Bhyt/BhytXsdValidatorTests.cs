using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.Bhyt;
using ProDiabHis.Application.Common;
using ProDiabHis.Infrastructure.Bhyt;
using ProDiabHis.UnitTests;
using Xunit;

namespace ProDiabHis.UnitTests.Bhyt;

/// <summary>
/// Test BhytXsdValidatorImpl VALIDATE THAT (khong con la placeholder gia). Dung ValidateXmlContent
/// (tach khoi I/O DB/storage) de test khong can Docker.
/// </summary>
public class BhytXsdValidatorTests
{
    private readonly BhytXsdValidatorImpl _validator;

    public BhytXsdValidatorTests()
    {
        // FakeEmptyDapperConnectionFactory: DbConnection that (khong phai mock IDbConnection),
        // luon tra ve rong -> mo phong dung truong hop "export chua duoc generate" (xml_file_path NULL).
        _validator = new BhytXsdValidatorImpl(
            NullLogger<BhytXsdValidatorImpl>.Instance,
            new FakeEmptyDapperConnectionFactory(),
            Substitute.For<IFileStorage>());
    }

    [Fact]
    public void ValidateXmlContent_XmlSinhTuSerializer_PassXsdThat()
    {
        // XML sinh boi BhytXmlSerializerImpl PHAI pass XSD that (khong phai gia lap).
        var serializer = new BhytXmlSerializerImpl();
        var items = new List<BhytExportItemData>
        {
            new(1, 0, "{\"MaLienKet\":\"BHYTIT01-1\",\"MaBenh\":\"E11.9\",\"NgaySinh\":\"1980-01-15\",\"NgayVao\":\"2026-05-10T08:00:00Z\"}",
                "BHYTIT01-1", "enc-1", null, 160000m),
            new(2, 0, "{\"MaLienKet\":\"BHYTIT01-1\",\"TenThuoc\":\"Metformin 500mg\",\"SoLuong\":30}",
                "BHYTIT01-1", "enc-1", null, 0m),
        };
        var xml = serializer.Serialize(1, "BHYTIT01", "2026-05", items);

        var result = _validator.ValidateXmlContent(xml);

        result.Valid.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateXmlContent_XmlKhongWellFormed_TraLoiRoRang()
    {
        var result = _validator.ValidateXmlContent("<GIAMDINHHS><Bang1>");

        result.Valid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Field == "xml");
    }

    [Fact]
    public async Task ValidateAsync_ChuaSinhXml_TraLoiRoRang_KhongNemException()
    {
        // Export chua duoc generate (xml_file_path = null trong DB that) -> validator phai bao loi
        // ro rang, KHONG duoc "log OK" gia nhu ban placeholder cu.
        var result = await _validator.ValidateAsync(exportId: 999, CancellationToken.None);

        result.Valid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Field == "xml_file_path");
    }
}
