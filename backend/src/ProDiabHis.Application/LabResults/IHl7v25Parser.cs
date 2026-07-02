namespace ProDiabHis.Application.LabResults;

/// <summary>Parse HL7 v2.5 ORU^R01 message sang list ImportRow</summary>
public interface IHl7v25Parser
{
    List<Hl7ParsedRow> Parse(string hl7Message);
}

public record Hl7ParsedRow(
    string?  LabOrderId,
    string   TestCode,
    string   Value,
    decimal? ValueNumeric,
    string?  Unit,
    DateTime PerformedAt);
