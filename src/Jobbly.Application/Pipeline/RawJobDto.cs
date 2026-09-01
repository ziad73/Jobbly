namespace Jobbly.Application.Pipeline;

// Provider-agnostic shape every IJobConnector emits. Composed from raw provider
// APIs (Greenhouse, Lever, ...) which never leak into the Application layer.
public sealed record RawJobDto(
    string ExternalId,
    string Title,
    string CompanyName,
    string? Location,
    string? Description,
    string SourceUrl,
    DateTime? PostedAt,
    int? SalaryMin = null,
    int? SalaryMax = null,
    string? SalaryCurrency = null,
    string? SalaryPeriod = null);