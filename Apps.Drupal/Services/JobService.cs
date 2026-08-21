using Apps.Drupal.Api;
using Apps.Drupal.Models.Requests;
using Apps.Drupal.Models.Responses;
using Blackbird.Applications.Sdk.Common.Authentication;
using RestSharp;

namespace Apps.Drupal.Services;

public class JobService(ApiClient client, IEnumerable<AuthenticationCredentialsProvider> credentials)
{
    public async Task<List<JobResponse>> SearchJobsAsync(SearchJobRequest filter)
    {
        var request = new ApiRequest("/api/tmgmt/blackbird/jobs", Method.Get, credentials);

        if (filter.State is not null)
        {
            request.AddQueryParameter("state", filter.State);
        }

        if (filter.CreatedAfter.HasValue)
        {
            var unixTimestamp = ((DateTimeOffset)filter.CreatedAfter.Value).ToUnixTimeSeconds();
            request.AddQueryParameter("created", unixTimestamp.ToString());
        }

        if (filter.TargetLanguage is not null)
        {
            request.AddQueryParameter("target", filter.TargetLanguage);
        }

        var jobs = await client.ExecuteWithErrorHandling<List<JobResponse>>(request) ?? [];
        foreach (var job in jobs)
        {
            job.Status = filter.State ?? "unprocessed";
        }

        return jobs;
    }
}
