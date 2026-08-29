using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace ProDiabHis.UnitTests.Common;

/// <summary>
/// Test chong tai phat (regression guard) cho lo ro ri du lieu cheo chi nhanh (docs/prd/
/// phan-tich-da-chi-nhanh-mo-rong-20260829.md muc E/Dot 0.b: "~10 Dapper handler chua loc branch
/// (cap phat thuoc, DTQG, BHYT export)").
///
/// KHONG the dung Testcontainers/DB that trong sandbox nay (khong co Docker/MySQL san sang) -> thay vi
/// mock IDbConnection (Dapper query tren mock rat cong kenh va gion), test nay doc TRUC TIEP source code
/// (.cs) cua tung handler da duoc fix va khang dinh:
///   1. Handler co inject IBranchProvider (constructor).
///   2. Than method Handle(...) co goi BranchSql.Condition(...) it nhat 1 lan.
/// Neu sau nay ai do vo tinh xoa branch filter (refactor, revert nham...) thi test se do ngay ma
/// khong can moi truong DB that. Day la gioi han da biet cua bai test nay — xem ghi chu class-level.
/// </summary>
public class BranchFilterRegressionTests
{
    // Tim thu muc source ProDiabHis.Application tu vi tri assembly test (bin/Debug/net8.0/...).
    private static string ApplicationSrcDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "backend")))
            dir = dir.Parent;

        dir.Should().NotBeNull("phai tim duoc thu muc goc repo chua 'backend' tu {0}", AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "backend", "src", "ProDiabHis.Application");
    }

    public static IEnumerable<object[]> FixedHandlerFiles()
    {
        yield return new object[] { Path.Combine("Pharmacy", "Dispensing", "DispensingHandlers.cs"),
            new[] { "GetDispenseQueueHandler", "GetDispenseHistoryHandler", "GetDispenseReceiptPdfHandler" } };
        yield return new object[] { Path.Combine("Pharmacy", "Dtqg", "DtqgHandlers.cs"),
            new[] { "GetDtqgStatusHandler", "ListDtqgSubmissionsHandler" } };
        yield return new object[] { Path.Combine("Pharmacy", "Dtqg", "SubmitDtqgFromPrescriptionHandler.cs"),
            new[] { "SubmitDtqgFromPrescriptionHandler" } };
        yield return new object[] { Path.Combine("Bhyt", "BhytExportQueries.cs"),
            new[] { "ListBhytExportsHandler", "GetBhytExportHandler", "DownloadBhytTableXmlHandler",
                     "ListBhytExportItemsHandler", "GetBhytExportItemHandler" } };
        yield return new object[] { Path.Combine("Bhyt", "BhytExportCommands.cs"),
            new[] { "DeleteBhytExportHandler", "GenerateBhytXmlHandler", "RegenerateBhytXmlHandler",
                     "ValidateBhytXmlHandler", "SignBhytXmlHandler", "SubmitBhytExportHandler" } };
    }

    [Theory]
    [MemberData(nameof(FixedHandlerFiles))]
    public void Handler_PhaiInjectIBranchProviderVaGoiBranchSqlCondition(string relativePath, string[] handlerClassNames)
    {
        var filePath = Path.Combine(ApplicationSrcDir(), relativePath);
        File.Exists(filePath).Should().BeTrue($"file '{filePath}' phai ton tai");
        var content = File.ReadAllText(filePath);

        foreach (var className in handlerClassNames)
        {
            // Cat rieng body cua 1 class (tu "class {Name}" den class ke tiep hoac het file) de tranh
            // nham lan voi cac class khac trong cung file.
            var startMatch = Regex.Match(content, $@"class\s+{Regex.Escape(className)}\b");
            startMatch.Success.Should().BeTrue($"khong tim thay class '{className}' trong '{relativePath}'");

            var nextClassMatch = Regex.Match(content[(startMatch.Index + 1)..], @"\npublic\s+(sealed\s+|abstract\s+)?(partial\s+)?class\s+\w+");
            var endIndex = nextClassMatch.Success
                ? startMatch.Index + 1 + nextClassMatch.Index
                : content.Length;

            var classBody = content[startMatch.Index..endIndex];

            classBody.Should().Contain("IBranchProvider",
                $"class '{className}' phai inject IBranchProvider de loc du lieu theo chi nhanh");
            classBody.Should().Contain("BranchSql.Condition",
                $"class '{className}' phai goi BranchSql.Condition(...) trong cau SQL de tranh ro ri cheo chi nhanh");
        }
    }
}
