using Apps.Drupal.Api;
using Apps.Drupal.Invocables;
using Apps.Drupal.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Drupal.DataSources;

public class LanguagesDataHandler(InvocationContext invocationContext)
    : AppInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var request = new ApiRequest("/api/tmgmt/blackbird/languages", Method.Get, Creds);
        var languages = await Client.ExecuteWithErrorHandling<List<LanguageDto>>(request) ?? [];

        return languages
            .Where(x => context.SearchString is null ||
                        x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(x => new DataSourceItem(x.Id, x.Name));
    }
}
