namespace ProDiabHis.Infrastructure.Pharmacy;

/// <summary>
/// Cấu hình tích hợp Đơn thuốc Quốc gia (ĐTQG - donthuocquocgia.vn) theo TT 27/2021/TT-BYT.
/// Bind từ section "Dtqg" trong appsettings. Khi <see cref="Enabled"/> = false (mặc định) hệ thống
/// dùng <see cref="MockDtqgClient"/> (dev/sandbox); khi true dùng <see cref="HttpDtqgClient"/> gọi cổng thật.
///
/// LƯU Ý: các đường dẫn dưới đây là giá trị mặc định hợp lý. Khi có tài liệu API chính thức của ĐTQG,
/// cần chỉnh lại path + schema payload/response cho khớp.
/// </summary>
public class DtqgOptions
{
    public const string SectionName = "Dtqg";

    /// <summary>Bật HTTP client thật (true) hay dùng mock (false, mặc định).</summary>
    public bool Enabled { get; set; }

    /// <summary>Base URL cổng ĐTQG.</summary>
    public string BaseUrl { get; set; } = "https://api.donthuocquocgia.vn";

    /// <summary>Token/API key Bearer mặc định. Production nên resolve token theo từng tenant.</summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>Đường dẫn gửi đơn (POST).</summary>
    public string SubmitPath { get; set; } = "/api/v1/don-thuoc";

    /// <summary>Đường dẫn tra trạng thái (GET). {ma} = ma_don_thuoc.</summary>
    public string StatusPath { get; set; } = "/api/v1/don-thuoc/{ma}/trang-thai";

    /// <summary>Đường dẫn huỷ đơn (POST). {ma} = ma_don_thuoc.</summary>
    public string CancelPath { get; set; } = "/api/v1/don-thuoc/{ma}/huy";

    /// <summary>Đường dẫn ping/health.</summary>
    public string PingPath { get; set; } = "/api/v1/ping";

    /// <summary>Timeout mỗi request (giây).</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
