using Apps.Drupal.Invocables;
using Apps.Drupal.Models.Requests;
using Apps.Drupal.Models.Responses;
using Apps.Drupal.Polling.Models;
using Apps.Drupal.Polling.Models.Requests;
using Apps.Drupal.Polling.Models.Responses;
using Apps.Drupal.Services;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Apps.Drupal.Polling;

[PollingEventList]
public class PollingList(InvocationContext invocationContext) : AppInvocable(invocationContext)
{
    [PollingEvent("On job statuses changed (deprecated)", Description = "Deprecated. Outputs jobs whose status changed since the previous poll")]
    public async Task<PollingEventResponse<JobStatusMemory, JobStatusChangedResponse>> OnJobStatusChanged(
        PollingEventRequest<JobStatusMemory> request,
        [PollingEventParameter] JobStatusChangedPollingParameters parameters)
    {
        var selectedStatuses = parameters.Statuses.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selectedStatuses.Count == 0)
        {
            throw new PluginMisconfigurationException("Select at least one status.");
        }

        var pollingTime = DateTime.UtcNow;
        var polledJobs = new List<JobResponse>();
        var jobService = new JobService(Client, Creds);
        foreach (var status in new[] { "active", "rejected", "aborted", "completed" })
        {
            polledJobs.AddRange(await jobService.SearchJobsAsync(new SearchJobRequest { State = status }));
        }

        var currentJobs = polledJobs
            .GroupBy(job => job.ContentId)
            .Select(group => group.Last())
            .ToList();
        var currentStatuses = request.Memory?.JobStatuses.ToDictionary() ?? [];
        foreach (var job in currentJobs)
        {
            currentStatuses[job.ContentId] = job.Status;
        }

        var memory = new JobStatusMemory
        {
            LastPollingTime = pollingTime,
            JobStatuses = currentStatuses
        };

        if (request.Memory is null)
        {
            return new PollingEventResponse<JobStatusMemory, JobStatusChangedResponse>
            {
                FlyBird = false,
                Memory = memory,
                Result = null
            };
        }

        var changedJobs = currentJobs
            .Where(job => request.Memory.JobStatuses.TryGetValue(job.ContentId, out var previousStatus) &&
                          !string.Equals(previousStatus, job.Status, StringComparison.OrdinalIgnoreCase) &&
                          selectedStatuses.Contains(job.Status) &&
                          (string.IsNullOrWhiteSpace(parameters.JobId) || job.ContentId == parameters.JobId) &&
                          (string.IsNullOrWhiteSpace(parameters.JobLabelContains) ||
                           job.Name.Contains(parameters.JobLabelContains, StringComparison.OrdinalIgnoreCase)))
            .Select(job => new JobStatusChangedItem
            {
                ContentId = job.ContentId,
                Name = job.Name,
                Source = job.Source,
                Target = job.Target,
                Status = job.Status,
                CreationDate = job.CreationDate,
                PreviousStatus = request.Memory.JobStatuses[job.ContentId]
            })
            .ToList();

        if (changedJobs.Count == 0)
        {
            return new PollingEventResponse<JobStatusMemory, JobStatusChangedResponse>
            {
                FlyBird = false,
                Memory = memory,
                Result = null
            };
        }

        return new PollingEventResponse<JobStatusMemory, JobStatusChangedResponse>
        {
            FlyBird = true,
            Memory = memory,
            Result = new JobStatusChangedResponse
            {
                Items = changedJobs,
                TotalCount = changedJobs.Count
            }
        };
    }

    [PollingEvent("On translation jobs requested (deprecated)", Description = "Deprecated. Outputs newly requested translation jobs")]
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
