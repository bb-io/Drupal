using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.DataSources.Static;

public class JobStateDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new[]
        {
            new DataSourceItem("active", "Active"),
            new DataSourceItem("completed", "Completed"),
            new DataSourceItem("aborted", "Aborted")
        };
    }
}
