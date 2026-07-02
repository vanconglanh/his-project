namespace ProDiabHis.Application.LabIntegration;

public record LabOutboundResponse(
    Guid     Id,
    Guid     LabOrderId,
    Guid     LabPartnerId,
    string?  PartnerName,
    string?  ExternalOrderId,
    object?  PayloadJson,
    string   Status,
    int      RetryCount,
    string?  ErrorMessage,
    DateTime? SentAt,
    DateTime? AckedAt,
    DateTime CreatedAt);

public record LabInboundResponse(
    Guid     Id,
    Guid     LabPartnerId,
    string?  PartnerName,
    string   ExternalResultId,
    Guid?    OutboundId,
    object?  PayloadJson,
    string?  RawHl7Message,
    string   Status,
    DateTime? ProcessedAt,
    DateTime ReceivedAt,
    int      ProcessedResultCount,
    string?  ErrorMessage);

public record SendToPartnerRequest(
    Guid   LabPartnerId,
    string Priority,
    string? Note);

public record WebhookInboundRequest(
    string ExternalOrderId,
    string ExternalResultId,
    List<WebhookResultItem> Results);

public record WebhookResultItem(
    string   TestCode,
    string   Value,
    decimal? ValueNumeric,
    string?  Unit,
    decimal? ReferenceRangeLow,
    decimal? ReferenceRangeHigh,
    string?  Flag,
    DateTime PerformedAt);

public record LabIntegrationStatsResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    int OutboundTotal,
    int OutboundFailed,
    int InboundTotal,
    int InboundFailed,
    List<PartnerStats> ByPartner);

public record PartnerStats(
    Guid   LabPartnerId,
    string PartnerName,
    int    OutboundSent,
    int    InboundReceived,
    double AvgTurnaroundMinutes);
