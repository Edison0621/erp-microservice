using Dapr.Client;

namespace ErpSystem.BuildingBlocks.Dapr;

public interface IDaprSecretsProvider
{
    Task<string?> GetSecretAsync(string storeName, string secretName, CancellationToken ct = default);
    Task<Dictionary<string, Dictionary<string, string>>> GetBulkSecretsAsync(string storeName, CancellationToken ct = default);
}

public class DaprSecretsProvider(DaprClient daprClient) : IDaprSecretsProvider
{
    public async Task<string?> GetSecretAsync(string storeName, string secretName, CancellationToken ct = default)
    {
        try
        {
            var secrets = await daprClient.GetSecretAsync(storeName, secretName, cancellationToken: ct);
            return secrets.TryGetValue(secretName, out var secret) ? secret : secrets.Values.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> GetBulkSecretsAsync(string storeName, CancellationToken ct = default)
    {
        try
        {
            return await daprClient.GetBulkSecretAsync(storeName, cancellationToken: ct);
        }
        catch
        {
            return new Dictionary<string, Dictionary<string, string>>();
        }
    }
}
