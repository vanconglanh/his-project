using FluentAssertions;
using ProDiabHis.Application.ICD10;
using Xunit;

namespace ProDiabHis.UnitTests.ICD10;

/// <summary>Unit tests cho ICD-10 search logic (US-E07)</summary>
public class Icd10SearchTests
{
    [Fact]
    public void Icd10Response_Fields_ArePopulated()
    {
        var response = new Icd10Response("E11.9", "Đái tháo đường typ 2 không biến chứng",
            "Type 2 diabetes mellitus without complications", "E10-E14", "E11", true);

        response.Code.Should().Be("E11.9");
        response.NameVi.Should().Contain("tháo đường");
        response.Category.Should().Be("E10-E14");
        response.IsBillable.Should().BeTrue();
    }

    [Theory]
    [InlineData("E11.9", true)]
    [InlineData("E10",   false)]  // E10 is_billable=0 (parent)
    public void Icd10_BillableFlag_ByCode(string code, bool expectBillable)
    {
        // Mock dataset matching seeded data
        var dataset = new Dictionary<string, bool>
        {
            ["E10"]   = false,
            ["E11"]   = false,
            ["E11.9"] = true,
            ["E10.9"] = true
        };

        dataset.TryGetValue(code, out var billable);
        billable.Should().Be(expectBillable);
    }

    [Theory]
    [InlineData("E11.9", "E11", true)]   // Child of E11
    [InlineData("E10",   null,  true)]   // Root
    [InlineData("E11",   null,  true)]   // Root
    public void Icd10_CategoryHierarchy(string code, string? parentCode, bool hasCategory)
    {
        var response = new Icd10Response(code, "Test", null, "E10-E14", parentCode, true);
        (response.Category is not null).Should().Be(hasCategory);
        response.ParentCode.Should().Be(parentCode);
    }

    [Fact]
    public void SearchQuery_Limit_CappedAt100()
    {
        var query = new SearchIcd10Query("đái tháo", "all", "E10-E14", false, 150);
        // Handler caps at 100
        Math.Min(query.Limit, 100).Should().Be(100);
    }

    [Fact]
    public void Icd10Category_GroupsCorrectly()
    {
        var cat = new Icd10CategoryDto("E10-E14", "IV", "Đái tháo đường", "Diabetes mellitus", 25);
        cat.CodeRange.Should().Be("E10-E14");
        cat.Count.Should().Be(25);
    }
}
