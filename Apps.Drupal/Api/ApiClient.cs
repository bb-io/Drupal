using Apps.Drupal.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using RestSharp;

namespace Apps.Drupal.Api;

public class ApiClient(IEnumerable<AuthenticationCredentialsProvider> credentials) : BlackBirdRestClient(new()
    { BaseUrl = new Uri(credentials.Get(CredsNames.BaseUrl).Value) })
{
    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var error = response.Content!;
        return new Exception($"Status code: {response.StatusCode}, Error: {error}");
    }
}