using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.DataSources.Static;

public class JobStateDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new[]
        {
            new DataSourceItem("unprocessed", "Unprocessed"),
            new DataSourceItem("active", "Active"),
            new DataSourceItem("rejected", "Rejected"),
            new DataSourceItem("aborted", "Aborted"),
            new DataSourceItem("completed", "Completed")
        };
    }
}
