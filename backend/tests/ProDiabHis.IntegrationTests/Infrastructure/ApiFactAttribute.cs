using Xunit;

namespace ProDiabHis.IntegrationTests.Infrastructure;

/// <summary>
/// [ApiFact] = [Fact] nhung tu dong SKIP (kem ly do that) khi may chay test khong co Docker,
/// vi toan bo integration test HTTP deu can MySQL container.
/// KHONG hard-code Skip — neu Docker co san thi test chay that.
/// </summary>
public sealed class ApiFactAttribute : FactAttribute
{
    public ApiFactAttribute()
    {
        Skip = DockerProbe.UnavailableReason;
    }
}

/// <summary>[ApiTheory] — ban Theory cua <see cref="ApiFactAttribute"/>.</summary>
public sealed class ApiTheoryAttribute : TheoryAttribute
{
    public ApiTheoryAttribute()
    {
        Skip = DockerProbe.UnavailableReason;
    }
}
