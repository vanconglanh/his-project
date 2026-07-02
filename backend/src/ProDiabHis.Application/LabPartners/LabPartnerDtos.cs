using System.Text.Json;

namespace ProDiabHis.Application.LabPartners;

public record LabPartnerResponse(
    Guid     Id,
    int      TenantId,
    string   Code,
    string   Name,
    string   EndpointUrl,
    string   AuthType,
    string?  ApiKeyMasked,
    string   Transport,
    List<string> SupportedTests,
    string   Status,
    string?  ContactEmail,
    string?  ContactPhone,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record LabPartnerCreateRequest(
    string   Code,
    string   Name,
    string   EndpointUrl,
    string   AuthType,
    string?  ApiKey,
    string?  BearerToken,
    string   Transport,
    List<string>? SupportedTests,
    string?  ContactEmail,
    string?  ContactPhone);

public record LabPartnerUpdateRequest(
    string?  Name,
    string?  EndpointUrl,
    string?  Transport,
    List<string>? SupportedTests,
    string?  Status,
    string?  ContactEmail,
    string?  ContactPhone);

public record LabPartnerCredentialsRequest(
    string   AuthType,
    string?  ApiKey,
    string?  BearerToken);

public record TestConnectionResponse(bool Ok, int LatencyMs, string Message);

public record RotateApiKeyResponse(string ApiKeyMasked, DateTime RotatedAt);
