using System.Linq;
using FluentAssertions;
using ProDiabHis.Application.LabResults;
using Xunit;

namespace ProDiabHis.UnitTests.LabResults;

/// <summary>
/// FIX 2026-10-16 (A4): cot diab_his_lab_results.result_flag o production la VARCHAR(5) nen cat cut
/// cac co hop le dai hon 5 ky tu ("NORMAL"->"NORMA", "CRITICAL"->"CRITI"). Migration 9220 noi cot len
/// VARCHAR(20). Test nay khoa bat bien: co dai nhat ma app sinh phai > 5 (chung minh VARCHAR(5) thieu)
/// va <= 20 (chung minh VARCHAR(20) du).
/// </summary>
public class LabResultFlagColumnWidthTests
{
    private static readonly string[] AllFlags =
    {
        LabResultFlag.Normal, LabResultFlag.H, LabResultFlag.L,
        LabResultFlag.HH, LabResultFlag.LL, LabResultFlag.Critical
    };

    [Fact]
    public void CoDaiNhat_Vuot5KyTu_NenVARCHAR5_CatCut()
    {
        AllFlags.Max(f => f.Length).Should().BeGreaterThan(5);
    }

    [Fact]
    public void MoiCo_NamGon_Trong20KyTu()
    {
        AllFlags.Should().OnlyContain(f => f.Length <= 20);
    }
}
