namespace Jobbly.Application.Pipeline;

// Provider-agnostic shape every IJobConnector emits. Composed from raw provider
// APIs (Greenhouse, Lever, ...) which never leak into the Application layer.
// RemoteHint carries a provider-supplied workplace signal (e.g. Ashby
// isRemote/workplaceType) that enrichment prefers over text rules; null means
// "no signal, infer from text".
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
    string? SalaryPeriod = null,
    string? RemoteHint = null);