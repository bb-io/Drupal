using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Drupal.DataSources.Static;

public class JobStateDataHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return new()
        {
            { "active", "Active" },
            { "completed", "Completed" }
        };
    }
}