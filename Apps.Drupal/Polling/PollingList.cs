using Apps.Drupal.Api;
using Apps.Drupal.Invocables;
using Apps.Drupal.Models.Responses;
using Apps.Drupal.Polling.Models;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Drupal.Polling;

[PollingEventList]
public class PollingList(InvocationContext invocationContext) : AppInvocable(invocationContext)
{
    [PollingEvent("On translation jobs requested",
        Description = "Returns translation jobs that were requested after the last polling time")]
    public async Task<PollingEventResponse<DateMemory, JobSearchResponse>> OnTranslationJobRequested(
        PollingEventRequest<DateMemory> request)
    {
        if(request.Memory == null)
        {
            return new PollingEventResponse<DateMemory, JobSearchResponse>
            {
                FlyBird = false,
                Memory = new DateMemory { LastPollingTime = DateTime.Now },
                Result = null
            };
        }
        
        var jobs = await SearchJobsAsync(request.Memory.LastPollingTime);

        return new PollingEventResponse<DateMemory, JobSearchResponse>
        {
            FlyBird = jobs.Total > 0,
            Memory = new DateMemory { LastPollingTime = DateTime.Now },
            Result = jobs
        };
    }
    
    private async Task<JobSearchResponse> SearchJobsAsync(DateTime lastPollingTime)
    {
        var unixTimestamp = ((DateTimeOffset)lastPollingTime).ToUnixTimeSeconds();
        
        var request = new ApiRequest("/api/tmgmt/blackbird/jobs", Method.Get, Creds)
            .AddQueryParameter("created", unixTimestamp.ToString());
        
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
}