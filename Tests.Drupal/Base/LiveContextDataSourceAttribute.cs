using System.Reflection;

namespace Tests.Drupal.Base;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LiveContextDataSourceAttribute : Attribute, ITestDataSource
{
    public IEnumerable<object[]> GetData(MethodInfo methodInfo) =>
        TestBase.LoadLiveContexts().Select(context => new object[] { context });

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data) =>
        data is null ? null : $"{methodInfo.Name} ({data[0]})";
}
