using Apps.Drupal.Api;
using Apps.Drupal.Invocables;
using Apps.Drupal.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Drupal.DataSources;

public class LanguagesDataHandler(InvocationContext invocationContext)
    : AppInvocable(invocationContext), IAsyncDataSourceHandler
{
    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var request = new ApiRequest("/api/tmgmt/blackbird/languages", Method.Get, Creds);
        var languages = await Client.ExecuteWithErrorHandling<Dictionary<string, LanguageDto>>(request);
        
        return languages
            .Where(x => context.SearchString == null || x.Value.Name.Contains(context.SearchString))
            .ToDictionary(x => x.Key, x => x.Value.Name);
    }
}