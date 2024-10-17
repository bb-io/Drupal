using Apps.Drupal.Api;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using RestSharp;

namespace Apps.Drupal.Connections;

public class ConnectionValidator: IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        var credentialsProviders = authenticationCredentialsProviders as AuthenticationCredentialsProvider[] ?? authenticationCredentialsProviders.ToArray();
        var client = new ApiClient(credentialsProviders);
        await client.ExecuteWithErrorHandling(new ApiRequest("/api/tmgmt/blackbird/languages", Method.Get, credentialsProviders));
        return new ConnectionValidationResponse()
        {
            IsValid = true
        };
    }
}