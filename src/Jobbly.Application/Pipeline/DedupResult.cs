namespace Jobbly.Application.Pipeline;

public sealed record DedupResult(bool IsDuplicate, Guid? CanonicalJobId = null);