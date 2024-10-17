using System.Text;
using Apps.Drupal.Api;
using Apps.Drupal.Invocables;
using Apps.Drupal.Models.Identifiers;
using Apps.Drupal.Models.Requests;
using Apps.Drupal.Models.Responses;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;

namespace Apps.Drupal.Actions;

[ActionList]
public class JobActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : AppInvocable(invocationContext)
{
    [Action("Search jobs", Description = "Get jobs by specified search parameters")]
    public async Task<JobSearchResponse> SearchJobsAsync([ActionParameter] SearchJobRequest filterRequest)
    {
        var request = new ApiRequest("/api/tmgmt/blackbird/jobs", Method.Get, Creds);

        if (filterRequest.State != null)
        {
            request.AddQueryParameter("state", filterRequest.State);
        }

        if (filterRequest.CreatedAfter.HasValue)
        {
            var unixTimestamp = ((DateTimeOffset)filterRequest.CreatedAfter.Value).ToUnixTimeSeconds();
            request.AddQueryParameter("created_after", unixTimestamp.ToString());
        }
        
        var jobs = await Client.ExecuteWithErrorHandling<Dictionary<string, JobResponse>>(request);
        
        return new JobSearchResponse
        {
            Items = jobs.Values.ToList(),
            Total = jobs.Count
        };
    }
    
    [Action("Get XLIFF from job", Description = "Get XLIFF file from job")]
    public async Task<FileReference> GetXliffFromJobAsync([ActionParameter] JobIdentifier identifier)
    {
        var request = new ApiRequest($"/api/tmgmt/blackbird/job/{identifier}", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling(request);
        var xliff = response.Content!;
        
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));
        return await fileManagementClient.UploadAsync(memoryStream, "application/xml", $"{identifier}.xliff");
    }
    
    [Action("Translate job", Description = "Translate job with specified job ID and XLIFF file")]
    public async Task TranslateJobAsync([ActionParameter] TranslateJobRequest request)
    {
        var stream = await fileManagementClient.DownloadAsync(request.File);
        var xliff = await new StreamReader(stream).ReadToEndAsync();

        var apiRequest = new ApiRequest($"/api/tmgmt/blackbird/job/{request.JobId}", Method.Post, Creds)
            .AddHeader("Content-Type", "application/xml")
            .AddBody(xliff);
        
        await Client.ExecuteWithErrorHandling(apiRequest);
    }
}