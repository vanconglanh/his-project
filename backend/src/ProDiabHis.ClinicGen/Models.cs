namespace ProDiabHis.ClinicGen;

// POCO khop answers.schema.json (JsonNamingPolicy.SnakeCaseLower map PascalCase -> snake_case)

public record Answers(
    Clinic Clinic,
    Deployment Deployment,
    Branding? Branding,
    string? SpecialtyPreset,
    List<Service>? Services,
    Admin Admin,
    List<Staff>? Staff,
    Facility? Facility,
    Smtp? Smtp,
    Modules? Modules);

public record Clinic(
    string Code, string Name, string? CompanyName, string? CskcbCode, string? TaxCode,
    string? Address, string? Phone, string? Email, string? Website, string? Slogan);

public record Deployment(
    string Domain, int? NginxPort, string? Timezone, string? Locale, string? Currency, string? SentryDsn);

public record Branding(string? Logo, string? PrimaryColor, string? AppDisplayName);

public record Service(string Code, string Name, decimal Price, int? Vat, string? BhytCode);

public record Admin(string FullName, string Email, string PasswordMode, string? Password);

public record Staff(
    string FullName, string Email, List<string> Roles, string? Phone,
    string PasswordMode, string? Password, string? Room, Doctor? Doctor);

public record Doctor(
    string? SoCchn, string? NoiCapCchn, string? NgayCapCchn, string? PhamViHanhNghe,
    string? ChuyenKhoa, List<object>? Schedule, List<string>? AllowedServices, decimal? CustomFee);

public record Facility(int? ExamRooms, int? ReceptionCounters, object? WorkingHours);

public record Smtp(string? Host, int? Port, string? User, string? Pass, string? From, bool? Ssl);

public record Modules(
    bool? Bhyt, bool? Dtqg, bool? Pharmacy, bool? Einvoice, bool? Cdss,
    bool? PatientPortal, bool? LabIntegration, bool? OnlineBooking, bool? SmsReminder, bool? ZaloOa);
