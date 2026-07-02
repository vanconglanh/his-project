namespace ProDiabHis.Application.Common;

/// <summary>Mask du lieu PII (CMND/CCCD/BHYT/phone/email) truoc khi tra ra ngoai</summary>
public interface IPiiMasker
{
    /// <summary>Mask CMND/CCCD: giu 2 chu dau + 2 chu cuoi, phan giua thay ***. Vi du: 07***12</summary>
    string MaskNationalId(string? value);

    /// <summary>Mask so BHYT: giu prefix 2 ky tu + 4 ky tu cuoi. Vi du: DN***5678</summary>
    string MaskBhyt(string? value);

    /// <summary>Mask so dien thoai: hien thi 3 so dau + 2 so cuoi. Vi du: 098***45</summary>
    string MaskPhone(string? value);

    /// <summary>Mask email: hien thi 2 ky tu dau cua local part + domain. Vi du: ng***@gmail.com</summary>
    string MaskEmail(string? value);

    /// <summary>Mask ho ten: hien thi ho + ten dem + chu cai dau cua ten. Vi du: Nguyen Van A.</summary>
    string MaskFullName(string? value);
}
