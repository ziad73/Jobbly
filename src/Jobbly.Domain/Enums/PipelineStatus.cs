namespace Jobbly.Domain.Enums;

public enum PipelineStatus
{
    Ingested = 1,
    Normalized = 2,
    Enriched = 3,
    Indexed = 4,
    Failed = 5
}
