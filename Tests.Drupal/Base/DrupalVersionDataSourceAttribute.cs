using System.Reflection;

namespace Tests.Drupal.Base;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DrupalVersionDataSourceAttribute : Attribute, ITestDataSource
{
    public IEnumerable<object[]> GetData(MethodInfo methodInfo) =>
        TestBase.SupportedVersions.Select(version => new object[] { version });

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data) =>
        data is null ? null : $"{methodInfo.Name} (Drupal {data[0]})";
}
