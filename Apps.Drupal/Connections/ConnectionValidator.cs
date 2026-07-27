using Apps.Drupal.Api;
using Apps.Drupal.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using RestSharp;

namespace Apps.Drupal.Connections;

public class ConnectionValidator : IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        var credentials = authenticationCredentialsProviders.ToArray();
        var client = new ApiClient(credentials);
        await client.ExecuteWithErrorHandling<List<LanguageDto>>(
            new ApiRequest("/api/tmgmt/blackbird/languages", Method.Get, credentials));

        return new ConnectionValidationResponse { IsValid = true };
    }
}
