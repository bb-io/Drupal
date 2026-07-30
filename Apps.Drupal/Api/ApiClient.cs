using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Apps.Drupal.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Polly;
using Polly.Retry;
using RestSharp;

namespace Apps.Drupal.Api;

public class ApiClient(IEnumerable<AuthenticationCredentialsProvider> credentials) : BlackBirdRestClient(new()
{
    BaseUrl = new Uri(credentials.Get(CredsNames.BaseUrl).Value)
})
{
    private const int RetryCount = 3;
    private static readonly Random Jitter = new();

    private readonly AsyncRetryPolicy<RestResponse> _retryPolicy = Policy
        .HandleResult<RestResponse>(r => r.StatusCode == HttpStatusCode.GatewayTimeout || r.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(RetryCount, attempt =>
            TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Jitter.Next(0, 500)),
            (_, _, _, _) => Task.CompletedTask);

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        var response = await _retryPolicy.ExecuteAsync(() => ExecuteAsync(request));
        if (!response.IsSuccessStatusCode)
            ConfigureErrorException(response);

        return response;
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var errorMessage = $"Status code: {(int)response.StatusCode}";

        if (!string.IsNullOrEmpty(response.Content))
        {
            string? extractedError = ExtractErrorMessage(response.Content, response.ContentType);
            if (!string.IsNullOrEmpty(extractedError))
            {
                errorMessage += $", Error: {extractedError}";
            }
            else
            {
                errorMessage += $", Error: {response.Content}";
            }
        }
        else if (!string.IsNullOrEmpty(response.StatusDescription))
        {
            errorMessage += $", Error: {response.StatusDescription}";
        }

        throw new PluginApplicationException(errorMessage);
    }
    
    private string? ExtractErrorMessage(string content, string? contentType)
    {
        if (contentType != null)
        {
            if (contentType.Contains("text/html"))
            {
                return ExtractErrorMessageFromHtml(content);
            }
        }

        return content;
    }

    private string? ExtractErrorMessageFromHtml(string htmlContent)
    {
        var titleMatch = Regex.Match(htmlContent, @"<title>\s*(.+?)\s*</title>", RegexOptions.IgnoreCase);
        if (titleMatch.Success)
        {
            return HttpUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim());
        }

        var bodyTextMatch = Regex.Match(htmlContent, @"<body[^>]*>(.*?)<\/body>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (bodyTextMatch.Success)
        {
            var bodyContent = bodyTextMatch.Groups[1].Value;
            var textContent = Regex.Replace(bodyContent, "<[^>]+>", " ").Trim();
            textContent = HttpUtility.HtmlDecode(textContent);

            if (!string.IsNullOrEmpty(textContent))
            {
                return textContent.Length > 200 ? textContent.Substring(0, 200) + "..." : textContent;
            }
        }

        return null;
    }
}