using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;
using System.IdentityModel.Tokens.Jwt;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/portal/v1")]
public class PatientPortalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPortalAuthService _portalAuth;
    private readonly ITenantProvider _tenant;

    public PatientPortalController(IMediator mediator, IPortalAuthService portalAuth, ITenantProvider tenant)
    {
        _mediator = mediator;
        _portalAuth = portalAuth;
        _tenant = tenant;
    }

    private Guid PatientId => Guid.Parse(User.FindFirst("patient_id")!.Value);
    private int TenantId => int.Parse(User.FindFirst("tenant_id")!.Value);

    // Tenant cua request an danh (login/activate): resolve tu subdomain -> diab_his_sys_tenants
    // (kien truc 1 DB dung chung). Dev cho phep override qua header X-Portal-Subdomain hoac ?clinic=.
    private async Task<int> ResolveTenantIdAsync(CancellationToken ct)
    {
        var host = Request.Host.Host;
        string? overrideSub = Request.Headers.TryGetValue("X-Portal-Subdomain", out var h) ? h.ToString() : null;
        if (string.IsNullOrWhiteSpace(overrideSub) && Request.Query.TryGetValue("clinic", out var c))
            overrideSub = c.ToString();
        return await _mediator.Send(new ResolvePortalTenantQuery(host, overrideSub), ct);
    }

    private IActionResult ErrTenantUnresolved()
        => StatusCode(400, new { error = new { code = "PORTAL_TENANT_UNRESOLVED", message = "Không xác định được phòng khám từ tên miền" } });

    // -------- Tenant info (cho man login: ten + logo + VAPID public key) --------
    [HttpGet("tenant-info")]
    [AllowAnonymous]
    public async Task<IActionResult> TenantInfo(CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (tenantId <= 0) return ErrTenantUnresolved();
        var info = await _mediator.Send(new GetPortalTenantInfoQuery(tenantId), cancellationToken);
        return Ok(new { data = info });
    }

    // -------- Auth: kich hoat + PIN (khong SMS) --------
    [HttpPost("auth/activate")]
    [AllowAnonymous]
    public async Task<IActionResult> Activate([FromBody] PortalActivateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (tenantId <= 0) return ErrTenantUnresolved();
        try
        {
            var result = await _mediator.Send(
                new PortalActivateCommand(request.Phone, request.ActivationCode, request.Pin, tenantId), cancellationToken);
            return Ok(new { data = result });
        }
        catch (PortalActivationInvalidException)
        {
            return BadRequest(new { error = new { code = "PORTAL_ACTIVATION_INVALID", message = "Mã kích hoạt không đúng hoặc đã hết hạn" } });
        }
        catch (PortalPinInvalidException)
        {
            return BadRequest(new { error = new { code = "PORTAL_PIN_INVALID", message = "Mã PIN phải gồm đúng 6 chữ số" } });
        }
    }

    [HttpPost("auth/login-pin")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginPin([FromBody] PortalPinLoginRequest request, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (tenantId <= 0) return ErrTenantUnresolved();
        try
        {
            var result = await _mediator.Send(
                new PortalPinLoginCommand(request.Phone, request.Pin, tenantId), cancellationToken);
            return Ok(new { data = result });
        }
        catch (PortalPhoneNotRegisteredException)
        {
            return NotFound(new { error = new { code = "PORTAL_PHONE_NOT_REGISTERED", message = "Số điện thoại chưa đăng ký. Vui lòng lấy mã kích hoạt tại quầy lễ tân." } });
        }
        catch (PortalNotActivatedException)
        {
            return BadRequest(new { error = new { code = "PORTAL_NOT_ACTIVATED", message = "Tài khoản chưa kích hoạt. Vui lòng kích hoạt bằng mã lễ tân cấp." } });
        }
        catch (PortalAccountLockedException)
        {
            return StatusCode(429, new { error = new { code = "PORTAL_ACCOUNT_LOCKED", message = "Nhập sai PIN quá 5 lần. Vui lòng thử lại sau 15 phút." } });
        }
        catch (PortalPinInvalidException)
        {
            return BadRequest(new { error = new { code = "PORTAL_PIN_INVALID", message = "Mã PIN không đúng" } });
        }
    }

    [HttpPost("auth/forgot-pin")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPin([FromBody] PortalForgotPinRequest request, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (tenantId <= 0) return ErrTenantUnresolved();
        await _mediator.Send(new PortalForgotPinCommand(request.Phone, tenantId), cancellationToken);
        // Luon tra 202 (khong tiet lo SDT/email co ton tai)
        return Accepted(new { data = new { message = "Nếu số điện thoại có đăng ký email, mã xác nhận đã được gửi." } });
    }

    [HttpPost("auth/reset-pin")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPin([FromBody] PortalResetPinRequest request, CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        if (tenantId <= 0) return ErrTenantUnresolved();
        try
        {
            var result = await _mediator.Send(
                new PortalResetPinCommand(request.Phone, request.Otp, request.NewPin, tenantId), cancellationToken);
            return Ok(new { data = result });
        }
        catch (OtpInvalidException)
        {
            return BadRequest(new { error = new { code = "PORTAL_OTP_INVALID", message = "Mã xác nhận không đúng" } });
        }
        catch (OtpExpiredException)
        {
            return StatusCode(410, new { error = new { code = "PORTAL_OTP_EXPIRED", message = "Mã xác nhận đã hết hạn" } });
        }
        catch (PortalPinInvalidException)
        {
            return BadRequest(new { error = new { code = "PORTAL_PIN_INVALID", message = "Mã PIN phải gồm đúng 6 chữ số" } });
        }
    }

    [HttpPost("auth/logout")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? "";
        var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        var expiresAt = expClaim != null
            ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime
            : DateTime.UtcNow.AddHours(24);

        await _mediator.Send(new PortalLogoutCommand(jti, expiresAt), cancellationToken);
        return NoContent();
    }

    // -------- Profile --------
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPortalMeQuery(PatientId, TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    // -------- Encounters --------
    [HttpGet("me/encounters")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetEncounters(
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _mediator.Send(
            new GetPortalEncountersQuery(PatientId, TenantId, page, page_size), cancellationToken);
        return Ok(new { data = items, meta = new { page, total } });
    }

    [HttpGet("me/encounters/{id:guid}")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetEncounterDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Application.PublicApi.GetPortalEncounterDetailQuery(id, PatientId, TenantId), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // -------- Prescriptions --------
    [HttpGet("me/prescriptions")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetPrescriptions(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Application.PublicApi.GetPortalPrescriptionsQuery(PatientId, TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    [HttpGet("me/prescriptions/{id:guid}/pdf")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetPrescriptionPdf(Guid id, CancellationToken cancellationToken)
    {
        // Ghi chu: PatientId/TenantId lay tu JWT claim cua PortalBearer (da hoat dong o cac
        // endpoint /me, /me/encounters, /me/appointments). Query rieng cho portal
        // (GetPortalPrescriptionPdfQuery) BAT BUOC loc them patient_id de benh nhan KHONG
        // xem duoc don thuoc cua benh nhan khac cung tenant.
        var result = await _mediator.Send(new Application.Pharmacy.Prescriptions.GetPortalPrescriptionPdfQuery(id, PatientId, TenantId), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return File(result.Value!, "application/pdf", $"don-thuoc-{id}.pdf");
    }

    // -------- Lab Results --------
    [HttpGet("me/lab-results")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetLabResults(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Application.PublicApi.GetPortalLabResultsQuery(PatientId, TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    // -------- Health trends (xu huong suc khoe cho Trang chu) --------
    [HttpGet("me/health-trends")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetHealthTrends(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Application.PublicApi.GetPortalHealthTrendsQuery(PatientId, TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    [HttpGet("me/lab-results/{id:guid}/pdf")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetLabResultPdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Application.LabResults.GetPortalLabResultPdfQuery(id, PatientId, TenantId), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return File(result.Value!, "application/pdf", $"ket-qua-xn-{id}.pdf");
    }

    // -------- Appointments --------
    [HttpGet("me/appointments")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetAppointments(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPortalAppointmentsQuery(PatientId, TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    [HttpPost("me/appointments")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] PortalAppointmentCreateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new CreatePortalAppointmentCommand(PatientId, TenantId, request), cancellationToken);
            return StatusCode(201, new { data = result });
        }
        catch (SlotTakenException)
        {
            return Conflict(new { error = new { code = "APPOINTMENT_SLOT_TAKEN", message = "Khung giờ đã được đặt hoặc ngoài lịch làm việc" } });
        }
        catch (AppointmentInPastException)
        {
            return BadRequest(new { error = new { code = "APPOINTMENT_IN_PAST", message = "Không thể đặt lịch ở thời điểm trong quá khứ" } });
        }
        catch (AppointmentDoctorRequiredException)
        {
            return BadRequest(new { error = new { code = "APPOINTMENT_DOCTOR_REQUIRED", message = "Vui lòng chọn bác sĩ" } });
        }
    }

    [HttpDelete("me/appointments/{id:guid}")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> CancelAppointment(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new CancelPortalAppointmentCommand(id, PatientId, TenantId), cancellationToken);
            return NoContent();
        }
        catch (AppointmentCancelTooLateException)
        {
            return BadRequest(new { error = new { code = "APPOINTMENT_CANCEL_TOO_LATE", message = "Khong the huy trong vong 2 gio truoc hen" } });
        }
        catch (AppointmentNotFoundException)
        {
            return NotFound(new { error = new { code = "APPOINTMENT_NOT_FOUND", message = "Khong tim thay lich hen" } });
        }
    }

    // -------- Queue (hang doi tiep don hom nay) --------
    [HttpGet("me/queue")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetQueueStatus(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Application.PublicApi.GetPortalQueueStatusQuery(PatientId, TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    // -------- Booking: doctors + slots --------
    [HttpGet("booking/doctors")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetBookingDoctors(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Application.PublicApi.GetPortalBookingDoctorsQuery(TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    [HttpGet("booking/slots")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetBookingSlots(
        [FromQuery] Guid doctor_ref, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Application.PublicApi.GetPortalBookingSlotsQuery(TenantId, doctor_ref, date), cancellationToken);
        return Ok(new { data = result });
    }

    // -------- Med reminders (nhac uong thuoc) --------
    [HttpGet("me/med-reminders")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetMedReminders(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Application.PublicApi.GetPortalMedRemindersQuery(PatientId, TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    [HttpPost("me/med-reminders/from-prescription/{prescriptionId:guid}")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> CreateMedRemindersFromPrescription(Guid prescriptionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new Application.PublicApi.CreateMedRemindersFromPrescriptionCommand(prescriptionId, PatientId, TenantId), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return StatusCode(201, new { data = result.Value });
    }

    [HttpPut("me/med-reminders/{id:guid}")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> UpdateMedReminder(Guid id, [FromBody] Application.PublicApi.UpdateMedReminderRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new Application.PublicApi.UpdateMedReminderEnabledCommand(id, PatientId, TenantId, body.Enabled), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // -------- Notification preferences + web push subscriptions --------
    [HttpGet("me/notification-preferences")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetNotificationPreferences(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Application.PublicApi.GetPortalNotifyPreferencesQuery(PatientId, TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    [HttpPut("me/notification-preferences")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> UpdateNotificationPreferences(
        [FromBody] Application.PublicApi.UpdatePortalNotifyPreferencesRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new Application.PublicApi.UpdatePortalNotifyPreferencesCommand(PatientId, TenantId, body), cancellationToken);
        return Ok(new { data = result });
    }

    [HttpPost("me/push-subscriptions")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> SubscribePush(
        [FromBody] Application.PublicApi.PortalPushSubscribeRequest body, CancellationToken cancellationToken)
    {
        await _mediator.Send(new Application.PublicApi.PortalPushSubscribeCommand(PatientId, TenantId, body), cancellationToken);
        return StatusCode(201, new { data = new { message = "Đã đăng ký nhận thông báo đẩy" } });
    }

    [HttpDelete("me/push-subscriptions")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> UnsubscribePush(
        [FromBody] Application.PublicApi.PortalPushUnsubscribeRequest body, CancellationToken cancellationToken)
    {
        await _mediator.Send(new Application.PublicApi.PortalPushUnsubscribeCommand(PatientId, TenantId, body.Endpoint), cancellationToken);
        return NoContent();
    }
}
