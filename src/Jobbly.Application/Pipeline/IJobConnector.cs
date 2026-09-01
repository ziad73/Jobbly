namespace Jobbly.Application.Pipeline;

// Port that provider connectors implement. One connector per provider API,
// identified by the slug the connector and the Provider row share.
public interface IJobConnector
{
    string ProviderSlug { get; }

    Task<IReadOnlyList<RawJobDto>> FetchAsync(CancellationToken cancellationToken = default);
}