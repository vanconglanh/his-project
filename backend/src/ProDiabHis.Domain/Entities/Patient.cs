using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Ho so benh nhan. Map bang pat_patients</summary>
public class Patient : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    /// <summary>CMND/CCCD da ma hoa AES-256-GCM</summary>
    public string? IdNumberEnc { get; set; }
    public string? IdNumberMasked { get; set; }
    /// <summary>Blind index HMAC-SHA256 cua CMND/CCCD (tra cuu exact-match)</summary>
    public string? IdNumberBidx { get; set; }
    /// <summary>So dien thoai — luu o cot phone_enc, ma hoa/giai ma tu dong qua EF value converter</summary>
    public string? Phone { get; set; }
    /// <summary>So dien thoai hien thi da che (vd 09****678)</summary>
    public string? PhoneMasked { get; set; }
    /// <summary>Blind index HMAC-SHA256 cua so dien thoai da chuan hoa</summary>
    public string? PhoneBidx { get; set; }
    public string? Email { get; set; }
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? WardCode { get; set; }
    /// <summary>Dia chi chi tiet — luu o cot street_enc (ma hoa)</summary>
    public string? Street { get; set; }
    public string? Occupation { get; set; }
    public string? Ethnicity { get; set; }
    public string? BloodType { get; set; }
    public string? AvatarUrl { get; set; }
    /// <summary>Ghi chu tiep don / benh an — luu o cot reception_note_enc (ma hoa)</summary>
    public string? ReceptionNote { get; set; }
    public string? AllergiesSummary { get; set; }
    public string Status { get; set; } = PatientStatus.Active;
    public DateOnly? IdCardIssuedDate { get; set; }
    public string? IdCardIssuedPlace { get; set; }
    public string Nationality { get; set; } = "VN";
    public string PatientType { get; set; } = "SERVICE";
    public string? MaritalStatus { get; set; }
    public string? VisitType { get; set; } = "FIRST_VISIT";
}

public static class PatientStatus
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
    public const string Deceased = "DECEASED";
}

public static class Gender
{
    public const string Male = "MALE";
    public const string Female = "FEMALE";
    public const string Other = "OTHER";
}
