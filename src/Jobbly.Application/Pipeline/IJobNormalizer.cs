using Jobbly.Domain.Entities;

namespace Jobbly.Application.Pipeline;

// Maps a provider-agnostic RawJobDto onto a freshly created Job entity, or
// refreshes an existing one (same provider + external id) from a newer fetch.
public interface IJobNormalizer
{
    Job Normalize(RawJobDto raw, Guid providerId);

    void Update(Job existing, RawJobDto raw);
}