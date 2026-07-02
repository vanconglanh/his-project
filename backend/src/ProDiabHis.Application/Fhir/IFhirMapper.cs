namespace ProDiabHis.Application.Fhir;

/// <summary>
/// Generic FHIR resource mapper interface.
/// TEntity: internal domain entity, TResource: Hl7.Fhir.Model resource type.
/// </summary>
public interface IFhirMapper<TEntity, TResource>
{
    TResource Map(TEntity entity);
}
