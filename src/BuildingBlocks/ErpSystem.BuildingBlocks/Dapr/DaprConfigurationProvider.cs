using Dapr.Client;

namespace ErpSystem.BuildingBlocks.Dapr;

public interface IDaprConfigurationProvider
{
    Task<string?> GetConfigurationAsync(string storeName, string key, CancellationToken ct = default);
}

public class DaprConfigurationProvider(DaprClient daprClient) : IDaprConfigurationProvider
{
    public async Task<string?> GetConfigurationAsync(string storeName, string key, CancellationToken ct = default)
    {
        try
        {
            var response = await daprClient.GetConfiguration(storeName, new List<string> { key }, cancellationToken: ct);
            return response.Items.TryGetValue(key, out var item) ? item.Value : null;
        }
        catch
        {
            return null;
        }
    }
}
