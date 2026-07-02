using NSubstitute;
using ProDiabHis.Application.Reports;
using ProDiabHis.Infrastructure.Reports;
using StackExchange.Redis;
using System.Text.RegularExpressions;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

/// <summary>
/// Kiem tra ReportCodeGenerator: format, seq tang dan.
/// Redis la bat buoc — test dung mock IConnectionMultiplexer.
/// </summary>
public class ReportCodeGeneratorTests
{
    private static readonly DateOnly Today = new DateOnly(2026, 5, 26);
    private const int TenantId = 99;

    // Pattern: RPT-{FIN|CLN|PHA}-{yyyyMMdd}-{D4}
    private static readonly Regex CodePattern =
        new(@"^RPT-(FIN|CLN|PHA)-\d{8}-\d{4,}$", RegexOptions.Compiled);

    /// <summary>Tao mock IConnectionMultiplexer + IDatabase voi INCR tang tu startSeq.</summary>
    private static IConnectionMultiplexer CreateMockRedis(long startSeq = 0)
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        redis.IsConnected.Returns(true);
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(db);

        // Mo phong INCR: moi lan goi StringIncrementAsync tra ve startSeq+1, +2, ...
        long counter = startSeq;
        db.StringIncrementAsync(Arg.Any<RedisKey>(), Arg.Any<long>(), Arg.Any<CommandFlags>())
            .Returns(_ => Task.FromResult(System.Threading.Interlocked.Increment(ref counter)));

        db.KeyExpireAsync(Arg.Any<RedisKey>(), Arg.Any<TimeSpan?>(), Arg.Any<ExpireWhen>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(true));

        return redis;
    }

    [Fact]
    public void Constructor_WhenRedisNull_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new ReportCodeGenerator(null!));
        Assert.Contains("redis", ex.ParamName);
    }

    [Fact]
    public async Task NextAsync_WhenRedisDisconnected_ThrowsInvalidOperationException()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.IsConnected.Returns(false);
        var gen = new ReportCodeGenerator(redis);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => gen.NextAsync(TenantId, ReportType.Financial, Today));
        Assert.Contains("Redis", ex.Message);
    }

    [Theory]
    [InlineData(ReportType.Financial, "FIN")]
    [InlineData(ReportType.Clinical,  "CLN")]
    [InlineData(ReportType.Pharmacy,  "PHA")]
    public async Task NextAsync_Returns_CorrectFormat(ReportType type, string expectedPrefix)
    {
        var gen = new ReportCodeGenerator(CreateMockRedis());
        var code = await gen.NextAsync(TenantId, type, Today);

        Assert.Matches(CodePattern, code);
        Assert.Contains(expectedPrefix, code);
        Assert.Contains("20260526", code);
    }

    [Fact]
    public async Task NextAsync_Sequential_Increments()
    {
        var gen = new ReportCodeGenerator(CreateMockRedis());

        var code1 = await gen.NextAsync(TenantId, ReportType.Financial, Today);
        var code2 = await gen.NextAsync(TenantId, ReportType.Financial, Today);
        var code3 = await gen.NextAsync(TenantId, ReportType.Financial, Today);

        long seq1 = long.Parse(code1.Split('-')[3]);
        long seq2 = long.Parse(code2.Split('-')[3]);
        long seq3 = long.Parse(code3.Split('-')[3]);

        Assert.True(seq2 == seq1 + 1, $"Seq2={seq2} phai bang seq1+1={seq1 + 1}");
        Assert.True(seq3 == seq2 + 1, $"Seq3={seq3} phai bang seq2+1={seq2 + 1}");
    }

    [Fact]
    public async Task NextAsync_DifferentTypes_IndependentSequences()
    {
        var gen = new ReportCodeGenerator(CreateMockRedis());

        var fin = await gen.NextAsync(TenantId, ReportType.Financial, Today);
        var cln = await gen.NextAsync(TenantId, ReportType.Clinical, Today);

        Assert.Contains("RPT-FIN-", fin);
        Assert.Contains("RPT-CLN-", cln);
    }

    [Fact]
    public async Task NextAsync_Returns_AtLeast4DigitSeq()
    {
        var gen = new ReportCodeGenerator(CreateMockRedis());
        var code = await gen.NextAsync(TenantId, ReportType.Pharmacy, Today);

        var seqPart = code.Split('-')[3];
        Assert.True(seqPart.Length >= 4, $"Seq '{seqPart}' phai co it nhat 4 chu so");
    }
}
