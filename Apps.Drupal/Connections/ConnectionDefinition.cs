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
            Description = "Base URL for the API",
            Sensitive = false
        },
        new ConnectionProperty(CredsNames.ApiKey)
        {
            DisplayName = "API key",
            Description = "Api key for the authentication",
            Sensitive = true
        }
    };
    
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = "Developer API key",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionUsage = ConnectionUsage.Actions,
            ConnectionProperties = ConnectionProperties
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values) =>
        values.Select(x =>
                new AuthenticationCredentialsProvider(AuthenticationCredentialsRequestLocation.None, x.Key, x.Value))
            .ToList();
}