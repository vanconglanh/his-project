using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// Portal: Danh sach nhac uong thuoc cua toi
// ============================================================
public record GetPortalMedRemindersQuery(Guid PatientId, int TenantId) : IRequest<List<PortalMedReminderResponse>>;

public class GetPortalMedRemindersHandler : IRequestHandler<GetPortalMedRemindersQuery, List<PortalMedReminderResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalMedRemindersHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<List<PortalMedReminderResponse>> Handle(GetPortalMedRemindersQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT id, drug_name, dose_label, time_slot, remind_time, start_date, end_date, enabled
              FROM diab_his_ptl_med_reminders
              WHERE tenant_id = @TenantId AND patient_id = @PatientId AND deleted_at IS NULL
              ORDER BY start_date DESC, remind_time ASC",
            new { q.TenantId, PatientId = q.PatientId.ToString() });

        return rows.Select(r => (PortalMedReminderResponse)MapRow(r)).ToList();
    }

    internal static PortalMedReminderResponse MapRow(dynamic r)
    {
        DateTime? endDate = r.end_date;
        bool enabled = Convert.ToBoolean(r.enabled);
        DateOnly? endDateOnly = endDate.HasValue ? DateOnly.FromDateTime(endDate.Value) : (DateOnly?)null;

        return new PortalMedReminderResponse(
            Guid.Parse((string)r.id),
            (string)r.drug_name,
            (string?)r.dose_label,
            (string)r.time_slot,
            TimeOnly.FromTimeSpan((TimeSpan)r.remind_time),
            DateOnly.FromDateTime((DateTime)r.start_date),
            endDateOnly,
            enabled);
    }
}

// ============================================================
// Portal: Sinh nhac uong thuoc tu 1 don thuoc
// ============================================================
public record CreateMedRemindersFromPrescriptionCommand(Guid PrescriptionId, Guid PatientId, int TenantId)
    : IRequest<Result<List<PortalMedReminderResponse>>>;

public class CreateMedRemindersFromPrescriptionHandler
    : IRequestHandler<CreateMedRemindersFromPrescriptionCommand, Result<List<PortalMedReminderResponse>>>
{
    private static readonly Dictionary<string, TimeSpan> SlotTimes = new()
    {
        ["SANG"] = new TimeSpan(7, 0, 0),
        ["TRUA"] = new TimeSpan(11, 30, 0),
        ["CHIEU"] = new TimeSpan(15, 0, 0),
        ["TOI"] = new TimeSpan(19, 0, 0),
    };

    private readonly IDapperConnectionFactory _db;
    public CreateMedRemindersFromPrescriptionHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result<List<PortalMedReminderResponse>>> Handle(
        CreateMedRemindersFromPrescriptionCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var prescriptionId = cmd.PrescriptionId.ToString();
        var patientId = cmd.PatientId.ToString();

        var prescription = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id FROM diab_his_pha_prescriptions
              WHERE id = @Id AND patient_id = @PatientId AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { Id = prescriptionId, PatientId = patientId, cmd.TenantId });

        if (prescription == null)
            return Result<List<PortalMedReminderResponse>>.Failure("PRESCRIPTION_NOT_FOUND", "Không tìm thấy đơn thuốc");

        var items = (await conn.QueryAsync<dynamic>(
            @"SELECT drug_name, dosage, frequency, duration_days
              FROM diab_his_pha_prescription_items
              WHERE prescription_id = @Id AND tenant_id = @TenantId",
            new { Id = prescriptionId, cmd.TenantId })).ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var created = new List<PortalMedReminderResponse>();

        foreach (var item in items)
        {
            var drugName = (string)item.drug_name;
            var doseLabel = (string?)item.dosage;
            var frequency = (string?)item.frequency ?? "";
            var durationDays = (int?)item.duration_days;
            var endDate = durationDays.HasValue ? today.AddDays(durationDays.Value) : (DateOnly?)null;

            var slots = MapFrequencyToSlots(frequency);

            foreach (var slot in slots)
            {
                var id = Guid.NewGuid();
                var remindTime = SlotTimes[slot];

                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_ptl_med_reminders
                        (id, tenant_id, patient_id, prescription_id, drug_name, dose_label,
                         time_slot, remind_time, start_date, end_date, enabled, created_at, updated_at)
                      VALUES (@Id, @TenantId, @PatientId, @PrescriptionId, @DrugName, @DoseLabel,
                              @TimeSlot, @RemindTime, @StartDate, @EndDate, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP())",
                    new
                    {
                        Id = id.ToString(), cmd.TenantId, PatientId = patientId, PrescriptionId = prescriptionId,
                        DrugName = drugName, DoseLabel = doseLabel, TimeSlot = slot, RemindTime = remindTime,
                        StartDate = today.ToString("yyyy-MM-dd"), EndDate = endDate?.ToString("yyyy-MM-dd")
                    });

                created.Add(new PortalMedReminderResponse(
                    id, drugName, doseLabel, slot, TimeOnly.FromTimeSpan(remindTime), today, endDate, true));
            }
        }

        return Result<List<PortalMedReminderResponse>>.Success(created);
    }

    internal static List<string> MapFrequencyToSlots(string frequency)
    {
        if (frequency.Contains("3 lần", StringComparison.OrdinalIgnoreCase))
            return new List<string> { "SANG", "TRUA", "TOI" };
        if (frequency.Contains("2 lần", StringComparison.OrdinalIgnoreCase))
            return new List<string> { "SANG", "TOI" };
        if (frequency.Contains("1 lần", StringComparison.OrdinalIgnoreCase))
            return new List<string> { "SANG" };
        return new List<string> { "SANG", "TOI" };
    }
}

// ============================================================
// Portal: Bat/tat 1 nhac uong thuoc
// ============================================================
public record UpdateMedReminderEnabledCommand(Guid Id, Guid PatientId, int TenantId, bool Enabled) : IRequest<Result>;

public class UpdateMedReminderEnabledHandler : IRequestHandler<UpdateMedReminderEnabledCommand, Result>
{
    private readonly IDapperConnectionFactory _db;
    public UpdateMedReminderEnabledHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result> Handle(UpdateMedReminderEnabledCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            @"UPDATE diab_his_ptl_med_reminders SET enabled = @Enabled, updated_at = UTC_TIMESTAMP()
              WHERE id = @Id AND patient_id = @PatientId AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { cmd.Enabled, Id = cmd.Id.ToString(), PatientId = cmd.PatientId.ToString(), cmd.TenantId });

        if (affected == 0)
            return Result.Failure("MED_REMINDER_NOT_FOUND", "Không tìm thấy lịch nhắc uống thuốc");

        return Result.Success();
    }
}
