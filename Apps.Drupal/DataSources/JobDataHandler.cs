using Apps.Drupal.Invocables;
using Apps.Drupal.Models.Requests;
using Apps.Drupal.Services;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Drupal.DataSources;

public class JobDataHandler(InvocationContext invocationContext)
    : AppInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var jobs = await new JobService(Client, Creds).SearchJobsAsync(new SearchJobRequest());

        return jobs
            .Where(x => context.SearchString is null ||
                        x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(x => new DataSourceItem(x.ContentId, x.Name));
    }
}
