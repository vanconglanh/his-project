namespace ProDiabHis.Application.Branches;

/// <summary>
/// Input thuan (khong phu thuoc DB) cho checklist go-live chi nhanh — BR-112/US-8.1.
/// Tach rieng de unit test khong can Testcontainers.
/// </summary>
public record BranchReadinessInput(
    int ExamRoomCount,
    int WarehouseCount,
    int DoctorCount,
    int ReceptionistCount,
    int UpcomingShiftCount,
    int CounterCount,
    bool BhytEnabled,
    bool HasCskcbCode,
    bool BhytContractValid,
    bool DtqgEnabled,
    bool DtqgConnected,
    bool DtqgTokenValid);

/// <summary>
/// Tinh checklist go-live chi nhanh (BR-112) tu du lieu da dem san — logic thuan, khong Dapper,
/// de handler (BranchHandlers.GetBranchReadinessHandler) va unit test cung goi lai duoc.
/// </summary>
public static class BranchReadinessCalculator
{
    public static List<ReadinessItemDto> Calculate(BranchReadinessInput input)
    {
        var items = new List<ReadinessItemDto>
        {
            new("room_exam", "Co it nhat 1 phong kham (EXAM)",
                input.ExamRoomCount > 0,
                input.ExamRoomCount > 0 ? $"Da co {input.ExamRoomCount} phong kham" : "Chua co phong kham nao"),

            new("warehouse", "Co it nhat 1 kho thuoc",
                input.WarehouseCount > 0,
                input.WarehouseCount > 0 ? $"Da co {input.WarehouseCount} kho" : "Chua co kho thuoc nao"),

            new("staff", "Co it nhat 1 bac si va 1 le tan duoc gan vao chi nhanh",
                input.DoctorCount > 0 && input.ReceptionistCount > 0,
                $"Bac si: {input.DoctorCount}, le tan: {input.ReceptionistCount}"),

            new("schedule", "Co it nhat 1 ca truc trong 7 ngay toi",
                input.UpcomingShiftCount > 0,
                input.UpcomingShiftCount > 0 ? $"Da co {input.UpcomingShiftCount} ca truc" : "Chua co lich truc nao trong 7 ngay toi"),

            new("counter", "Da co bo dem so phieu",
                input.CounterCount > 0,
                input.CounterCount > 0 ? $"Da co {input.CounterCount} bo dem" : "Chua cau hinh bo dem so phieu"),

            new("einvoice", "Hoa don dien tu",
                true,
                "Khong ap dung — bo theo quyet dinh Q3 (khong lam HDDT theo Facility)"),
        };

        if (input.BhytEnabled)
        {
            var bhytOk = input.HasCskcbCode && input.BhytContractValid;
            items.Add(new ReadinessItemDto("bhyt", "Cau hinh BHYT (ma CSKCB + hop dong con hieu luc)",
                bhytOk,
                bhytOk
                    ? "Da co ma CSKCB va hop dong BHYT con hieu luc"
                    : (!input.HasCskcbCode ? "Chua co ma CSKCB" : "Hop dong BHYT chua co/da het hieu luc")));
        }

        if (input.DtqgEnabled)
        {
            var dtqgOk = input.DtqgConnected && input.DtqgTokenValid;
            items.Add(new ReadinessItemDto("dtqg", "Ket noi Don thuoc Quoc gia (token con han)",
                dtqgOk,
                dtqgOk ? "Da ket noi va token con han" : "Chua co credential hoac token da het han"));
        }

        return items;
    }

    public static BranchReadinessDto Build(int branchId, BranchReadinessInput input)
    {
        var items = Calculate(input);
        return new BranchReadinessDto(branchId, items.All(i => i.Passed), items);
    }
}
