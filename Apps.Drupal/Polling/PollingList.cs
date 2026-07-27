using Apps.Drupal.Invocables;
using Apps.Drupal.Models.Requests;
using Apps.Drupal.Models.Responses;
using Apps.Drupal.Polling.Models;
using Apps.Drupal.Polling.Models.Requests;
using Apps.Drupal.Services;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Blackbird.Applications.SDK.Blueprints;

namespace Apps.Drupal.Polling;

[PollingEventList]
public class PollingList(InvocationContext invocationContext) : AppInvocable(invocationContext)
{
    [PollingEvent("On translation jobs requested", Description = "Outputs newly requested translation jobs")]
    [BlueprintEventDefinition(BlueprintEvent.ContentCreatedOrUpdatedMultiple)]
    public async Task<PollingEventResponse<DateMemory, JobSearchResponse>> OnTranslationJobRequested(
        PollingEventRequest<DateMemory> request,
        [PollingEventParameter] TranslationJobsPollingParameters parameters)
    {
        var pollingTime = DateTime.UtcNow;
        if (request.Memory is null)
        {
            return new PollingEventResponse<DateMemory, JobSearchResponse>
            {
                FlyBird = false,
                Memory = new DateMemory { LastPollingTime = pollingTime },
                Result = null
            };
        }

        var jobs = await new JobService(Client, Creds).SearchJobsAsync(new SearchJobRequest
        {
            CreatedAfter = request.Memory.LastPollingTime
        });

        if (parameters.TargetLanguages is not null)
        {
            var targetLanguages = parameters.TargetLanguages.ToHashSet(StringComparer.OrdinalIgnoreCase);
            jobs = jobs.Where(job => targetLanguages.Contains(job.Target)).ToList();
        }

        if (jobs.Count == 0)
        {
            return new PollingEventResponse<DateMemory, JobSearchResponse>
            {
                FlyBird = false,
                Memory = new DateMemory { LastPollingTime = pollingTime },
                Result = null
            };
        }

        return new PollingEventResponse<DateMemory, JobSearchResponse>
        {
            FlyBird = true,
            Memory = new DateMemory { LastPollingTime = pollingTime },
            Result = new JobSearchResponse
            {
                Items = jobs,
                TotalCount = jobs.Count
            }
        };
    }
}
