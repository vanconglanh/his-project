using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.Pharmacy;
using ProDiabHis.Infrastructure.Pharmacy;
using Xunit;

namespace ProDiabHis.UnitTests.Pharmacy;

/// <summary>
/// Unit test cho <see cref="HttpDtqgClient"/> — dùng stub <see cref="HttpMessageHandler"/> để
/// giả lập phản hồi cổng ĐTQG, kiểm tra parse ma_don_thuoc, xử lý lỗi HTTP, thay {ma} trong path,
/// và bóc trường trong "data" bọc ngoài. KHÔNG gọi mạng thật.
/// </summary>
public class HttpDtqgClientTests
{
    private static readonly DtqgSubmitPayload SamplePayload =
        new(TenantId: 1, PrescriptionId: 123, CskcbId: "79123", PartnerCode: "PARTNER01",
            PrescriptionData: new { thuoc = "Metformin 500mg" });

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (HttpDtqgClient client, StubHandler handler) Build(
        HttpStatusCode status, string body, DtqgOptions? opt = null, IDtqgCredentialProvider? creds = null)
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://dtqg.test") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(http);
        if (creds is null)
        {
            creds = Substitute.For<IDtqgCredentialProvider>();
            creds.GetForCurrentTenantAsync(Arg.Any<CancellationToken>()).Returns((DtqgTenantCredentials?)null);
        }
        var client = new HttpDtqgClient(factory, opt ?? new DtqgOptions(), creds, NullLogger<HttpDtqgClient>.Instance);
        return (client, handler);
    }

    [Fact]
    public async Task Submit_success_parses_ma_don_thuoc()
    {
        var (client, _) = Build(HttpStatusCode.OK, "{\"ma_don_thuoc\":\"VN260102000ABC\"}");

        var res = await client.SubmitPrescriptionAsync(SamplePayload);

        res.Success.Should().BeTrue();
        res.MaDonThuoc.Should().Be("VN260102000ABC");
        res.ErrorCode.Should().BeNull();
    }

    [Fact]
    public async Task Submit_parses_ma_don_thuoc_wrapped_in_data()
    {
        var (client, _) = Build(HttpStatusCode.OK, "{\"data\":{\"ma_don_thuoc\":\"WRAP123456789\"}}");

        var res = await client.SubmitPrescriptionAsync(SamplePayload);

        res.Success.Should().BeTrue();
        res.MaDonThuoc.Should().Be("WRAP123456789");
    }

    [Fact]
    public async Task Submit_http_error_returns_failure_with_status_code()
    {
        var (client, _) = Build(HttpStatusCode.BadRequest, "{\"loi\":\"du lieu sai\"}");

        var res = await client.SubmitPrescriptionAsync(SamplePayload);

        res.Success.Should().BeFalse();
        res.ErrorCode.Should().Be("HTTP_400");
    }

    [Fact]
    public async Task Submit_missing_ma_don_thuoc_returns_failure()
    {
        var (client, _) = Build(HttpStatusCode.OK, "{\"foo\":\"bar\"}");

        var res = await client.SubmitPrescriptionAsync(SamplePayload);

        res.Success.Should().BeFalse();
        res.ErrorCode.Should().Be("NO_MA_DON_THUOC");
    }

    [Fact]
    public async Task Submit_sends_json_body_with_cskcb_and_partner()
    {
        var (client, handler) = Build(HttpStatusCode.OK, "{\"ma_don_thuoc\":\"VN000000000001\"}");

        await client.SubmitPrescriptionAsync(SamplePayload);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequestBody.Should().Contain("79123");        // co_so_kham_chua_benh_id
        handler.LastRequestBody.Should().Contain("PARTNER01");     // ma_doi_tac
    }

    [Fact]
    public async Task GetStatus_parses_trang_thai_and_substitutes_ma_in_path()
    {
        var (client, handler) = Build(HttpStatusCode.OK, "{\"trang_thai\":\"ACCEPTED\"}");

        var res = await client.GetStatusAsync("VN12345");

        res.Status.Should().Be("ACCEPTED");
        res.MaDonThuoc.Should().Be("VN12345");
        handler.LastRequest!.RequestUri!.ToString().Should().Contain("VN12345");
        handler.LastRequest.RequestUri.ToString().Should().Contain("/trang-thai");
    }

    [Fact]
    public async Task GetStatus_http_error_returns_unknown()
    {
        var (client, _) = Build(HttpStatusCode.InternalServerError, "boom");

        var res = await client.GetStatusAsync("VN9");

        res.Status.Should().Be("UNKNOWN");
        res.ErrorCode.Should().Be("HTTP_500");
    }

    [Fact]
    public async Task Cancel_returns_true_on_success_and_sends_reason()
    {
        var (client, handler) = Build(HttpStatusCode.OK, "{}");

        var ok = await client.CancelAsync("VN9", "het han su dung");

        ok.Should().BeTrue();
        handler.LastRequestBody.Should().Contain("het han su dung"); // ly_do
    }

    [Fact]
    public async Task Cancel_returns_false_on_http_error()
    {
        var (client, _) = Build(HttpStatusCode.Forbidden, "{}");

        var ok = await client.CancelAsync("VN9", "ly do");

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task Ping_returns_ok_and_nonnegative_latency()
    {
        var (client, _) = Build(HttpStatusCode.OK, "{\"ok\":true}");

        var res = await client.PingAsync();

        res.Ok.Should().BeTrue();
        (res.LatencyMs >= 0).Should().BeTrue();
    }

    [Fact]
    public async Task Submit_uses_per_tenant_token_for_authorization()
    {
        var creds = Substitute.For<IDtqgCredentialProvider>();
        creds.GetForCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new DtqgTenantCredentials("79999", "PARTNER_T", "tenant-token-xyz"));
        var (client, handler) = Build(HttpStatusCode.OK, "{\"ma_don_thuoc\":\"VN000000000009\"}", creds: creds);

        await client.SubmitPrescriptionAsync(SamplePayload);

        handler.LastRequest!.Headers.Authorization.Should().NotBeNull();
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("tenant-token-xyz");
    }

    [Fact]
    public async Task Submit_falls_back_to_tenant_cskcb_and_partner_when_payload_empty()
    {
        var creds = Substitute.For<IDtqgCredentialProvider>();
        creds.GetForCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new DtqgTenantCredentials("79999", "PARTNER_T", "tk"));
        var (client, handler) = Build(HttpStatusCode.OK, "{\"ma_don_thuoc\":\"VN000000000009\"}", creds: creds);

        var emptyPayload = new DtqgSubmitPayload(1, 5, "", "", new { });
        await client.SubmitPrescriptionAsync(emptyPayload);

        handler.LastRequestBody.Should().Contain("79999");     // cskcb_id lấy từ credential provider
        handler.LastRequestBody.Should().Contain("PARTNER_T"); // partner_code lấy từ credential provider
    }
}
