using Apps.Drupal.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.Drupal.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    private static IEnumerable<ConnectionProperty> ConnectionProperties => new[]
    {
        new ConnectionProperty(CredsNames.BaseUrl)
        {
            DisplayName = "Base URL",
            Description = "URL of Drupal site, for example https://drupal.example.com",
            Sensitive = false
        },
        new ConnectionProperty(CredsNames.ApiKey)
        {
            DisplayName = "API key",
            Description = "API key from Blackbird translator configuration",
            Sensitive = true
        }
    };
    
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = "API key",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = ConnectionProperties
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(Dictionary<string, string> values) =>
        values.Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)).ToList();
}
