using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Metadata;

namespace Apps.Drupal;

public class Application : IApplication, ICategoryProvider
{
    public string Name
    {
        get => "Drupal";
        set { }
    }

    public T GetInstance<T>()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ApplicationCategory> Categories
    {
        get => new [] { ApplicationCategory.Cms };
        set { }
    }
}