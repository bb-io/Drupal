using System.Text;
using Apps.Drupal.Api;
using Apps.Drupal.Invocables;
using Apps.Drupal.Models.Identifiers;
using Apps.Drupal.Models.Requests;
using Apps.Drupal.Models.Responses;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
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
            request.AddQueryParameter("created", unixTimestamp.ToString());
        }
        
        if(filterRequest.TargetLanguage != null)
        {
            request.AddQueryParameter("target", filterRequest.TargetLanguage);
        }
        
        var jobsResponse = await Client.ExecuteWithErrorHandling(request);
        
        var jobs = new List<JobResponse>();
        
        if(jobsResponse.Content!.StartsWith("{") && jobsResponse.Content!.EndsWith("}")) // If there is no jobs, the response will be a collection of objects, if response contains jobs, it will be a dictionary
        {
            var deserializedResponse = JsonConvert.DeserializeObject<Dictionary<string, JobResponse>>(jobsResponse.Content)!;
            jobs = deserializedResponse.Values.ToList();
        }
        
        return new JobSearchResponse
        {
            Items = jobs,
            Total = jobs.Count
        };
    }
    
    [Action("Get job as HTML", Description = "Get HTML file from the job with specified job ID")]
    public async Task<GetXliffFromJobResponse> GetXliffFromJobAsync([ActionParameter] JobIdentifier identifier)
    {
        var request = new ApiRequest($"/api/tmgmt/blackbird/job/{identifier}", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling(request);
        var xliff = response.Content!;
        
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));
        return new()
        {
            File = await fileManagementClient.UploadAsync(memoryStream, "text/html", $"{identifier}.html")
        };
    }
    
    [Action("Update job from HTML", Description = "Update job from HTML file")]
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