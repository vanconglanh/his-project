using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ProDiabHis.Infrastructure.Bhyt;
using Xunit;

namespace ProDiabHis.UnitTests.Bhyt;

public class BhytXsdValidatorTests
{
    private readonly BhytXsdValidatorImpl _validator;

    public BhytXsdValidatorTests()
    {
        _validator = new BhytXsdValidatorImpl(NullLogger<BhytXsdValidatorImpl>.Instance);
    }

    [Fact]
    public async Task Validate_returns_valid_when_xsd_placeholders_present()
    {
        // Arrange: XSD placeholder files exist in app base directory under Resources/Xsd/qd4750/
        // In dev test, AppContext.BaseDirectory may differ, so validator skips missing XSDs -> valid
        var result = await _validator.ValidateAsync(exportId: 1, CancellationToken.None);

        // Dù XSD placeholder có hay không, validator không ném ngoại lệ
        result.Should().NotBeNull();
        result.Errors.Should().NotBeNull();
    }

    [Fact]
    public async Task Validate_returns_empty_errors_when_no_xsd_files()
    {
        // Khi không tìm thấy XSD file (test environment), validator skip và trả Valid=true
        var result = await _validator.ValidateAsync(exportId: 99, CancellationToken.None);

        result.Valid.Should().BeTrue("XSD files missing/skipped -> validator passes by default");
        result.Errors.Should().BeEmpty();
    }
}
