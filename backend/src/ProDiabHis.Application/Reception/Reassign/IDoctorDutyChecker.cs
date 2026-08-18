namespace ProDiabHis.Application.Reception.Reassign;

/// <summary>Ket qua kiem tra lich truc bac si tai thoi diem dieu phoi (CANH BAO, khong chan).</summary>
/// <param name="OnDuty">Bac si co lich truc khung gio hien tai</param>
/// <param name="Blocked">Bac si dang bi block (nghi phep / hop / ngay le)</param>
/// <param name="LocalTimeLabel">Nhan thoi gian theo gio Viet Nam de hien thi canh bao</param>
public record DoctorDutyStatus(bool OnDuty, bool Blocked, string LocalTimeLabel);

/// <summary>[G05] Kiem tra lich truc bac si dich khi dieu phoi luot kham.</summary>
public interface IDoctorDutyChecker
{
    /// <summary>Kiem tra bac si co truc / co bi block tai thoi diem <paramref name="atUtc"/>.</summary>
    Task<DoctorDutyStatus> CheckAsync(int tenantId, Guid doctorId, DateTime atUtc, CancellationToken ct = default);
}
