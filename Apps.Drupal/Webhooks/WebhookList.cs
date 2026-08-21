using System.Net;
using System.Security.Cryptography;
using System.Text;
using Apps.Drupal.Constants;
using Apps.Drupal.Invocables;
using Apps.Drupal.Models.Requests;
using Apps.Drupal.Models.Responses;
using Apps.Drupal.Polling.Models.Requests;
using Apps.Drupal.Polling.Models.Responses;
using Apps.Drupal.Services;
using Apps.Drupal.Webhooks.Handlers;
using Apps.Drupal.Webhooks.Models;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.SDK.Blueprints;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonElement = System.Text.Json.JsonElement;

namespace Apps.Drupal.Webhooks;

[WebhookList("Translation jobs")]
public class WebhookList(InvocationContext invocationContext) : AppInvocable(invocationContext)
{
    [BlueprintEventDefinition(BlueprintEvent.ContentCreatedOrUpdatedMultiple)]
    [Webhook("On translation jobs requested", typeof(TranslationJobsSubmittedHandler),
        Description = "Triggered when translation jobs are requested")]
    public async Task<WebhookResponse<JobSearchResponse>> OnTranslationJobsRequested(
        WebhookRequest webhookRequest,
        [WebhookParameter] TranslationJobsPollingParameters parameters)
    {
        var body = webhookRequest.Body switch
        {
            string rawBody => rawBody,
            JsonElement jsonElement => jsonElement.GetRawText(),
            JToken jsonToken => jsonToken.ToString(Formatting.None),
            null => null,
            _ => webhookRequest.Body.ToString()
        };
        if (string.IsNullOrWhiteSpace(body))
        {
            return BadRequest<JobSearchResponse>("Webhook body is empty.");
        }

        var signature = webhookRequest.Headers
            .FirstOrDefault(header => header.Key.Equals("X-Blackbird-Signature", StringComparison.OrdinalIgnoreCase))
            .Value;
        if (string.IsNullOrWhiteSpace(signature) ||
            !signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest<JobSearchResponse>("Webhook signature is missing or invalid.");
        }

        byte[] suppliedSignature;
        try
        {
            suppliedSignature = Convert.FromHexString(signature["sha256=".Length..]);
        }
        catch (FormatException)
        {
            return BadRequest<JobSearchResponse>("Webhook signature is missing or invalid.");
        }

        var signingKey = SHA256.HashData(Encoding.UTF8.GetBytes(Creds.Get(CredsNames.ApiKey).Value));
        var expectedSignature = HMACSHA256.HashData(signingKey, Encoding.UTF8.GetBytes(body));
        if (!CryptographicOperations.FixedTimeEquals(expectedSignature, suppliedSignature))
        {
            return BadRequest<JobSearchResponse>("Webhook signature is missing or invalid.");
        }

        DrupalWebhookPayload? payload;
        try
        {
            payload = JsonConvert.DeserializeObject<DrupalWebhookPayload>(body);
        }
        catch (JsonException)
        {
            return BadRequest<JobSearchResponse>("Webhook body is invalid.");
        }

        if (payload is null || payload.Jobs is null)
        {
            return BadRequest<JobSearchResponse>("Webhook body is invalid.");
        }

        if (!string.Equals(payload.Event, WebhookTopics.TranslationJobsSubmitted, StringComparison.Ordinal))
        {
            return Preflight<JobSearchResponse>();
        }

        var targetLanguages = parameters.TargetLanguages?
            .Where(language => !string.IsNullOrWhiteSpace(language))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var eventJobs = payload.Jobs
            .Where(job => targetLanguages is null || targetLanguages.Count == 0 ||
                          targetLanguages.Contains(job.TargetLanguage))
            .ToList();
        if (eventJobs.Count == 0)
        {
            return Preflight<JobSearchResponse>();
        }

        var currentJobs = await new JobService(Client, Creds)
            .SearchJobsAsync(new SearchJobRequest { State = "active" });
        var currentJobsById = currentJobs.ToDictionary(job => job.ContentId);
        var jobs = eventJobs.Select(job => currentJobsById.TryGetValue(job.JobId, out var currentJob)
                ? currentJob
                : new JobResponse
                {
                    ContentId = job.JobId,
                    Source = job.SourceLanguage,
                    Target = job.TargetLanguage,
                    Status = job.Status
                })
            .ToList();

        return new WebhookResponse<JobSearchResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK),
            Result = new JobSearchResponse
            {
                Items = jobs,
                TotalCount = jobs.Count
            }
        };
    }

    [Webhook("On job statuses changed", typeof(TranslationJobStatusChangedHandler),
        Description = "Triggered when translation job statuses change")]
    public async Task<WebhookResponse<JobStatusChangedResponse>> OnJobStatusesChanged(
        WebhookRequest webhookRequest,
        [WebhookParameter] JobStatusChangedPollingParameters parameters)
    {
        var body = webhookRequest.Body switch
        {
            string rawBody => rawBody,
            JsonElement jsonElement => jsonElement.GetRawText(),
            JToken jsonToken => jsonToken.ToString(Formatting.None),
            null => null,
            _ => webhookRequest.Body.ToString()
        };
        if (string.IsNullOrWhiteSpace(body))
        {
            return BadRequest<JobStatusChangedResponse>("Webhook body is empty.");
        }

        var signature = webhookRequest.Headers
            .FirstOrDefault(header => header.Key.Equals("X-Blackbird-Signature", StringComparison.OrdinalIgnoreCase))
            .Value;
        if (string.IsNullOrWhiteSpace(signature) ||
            !signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest<JobStatusChangedResponse>("Webhook signature is missing or invalid.");
        }

        byte[] suppliedSignature;
        try
        {
            suppliedSignature = Convert.FromHexString(signature["sha256=".Length..]);
        }
        catch (FormatException)
        {
            return BadRequest<JobStatusChangedResponse>("Webhook signature is missing or invalid.");
        }

        var signingKey = SHA256.HashData(Encoding.UTF8.GetBytes(Creds.Get(CredsNames.ApiKey).Value));
        var expectedSignature = HMACSHA256.HashData(signingKey, Encoding.UTF8.GetBytes(body));
        if (!CryptographicOperations.FixedTimeEquals(expectedSignature, suppliedSignature))
        {
            return BadRequest<JobStatusChangedResponse>("Webhook signature is missing or invalid.");
        }

        DrupalWebhookPayload? payload;
        try
        {
            payload = JsonConvert.DeserializeObject<DrupalWebhookPayload>(body);
        }
        catch (JsonException)
        {
            return BadRequest<JobStatusChangedResponse>("Webhook body is invalid.");
        }

        if (payload is null || payload.Jobs is null)
        {
            return BadRequest<JobStatusChangedResponse>("Webhook body is invalid.");
        }

        if (!string.Equals(payload.Event, WebhookTopics.TranslationJobStatusChanged, StringComparison.Ordinal))
        {
            return Preflight<JobStatusChangedResponse>();
        }

        var selectedStatuses = (parameters.Statuses ?? [])
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selectedStatuses.Count == 0)
        {
            throw new PluginMisconfigurationException("Select at least one status.");
        }

        var eventJobs = payload.Jobs
            .Where(job => selectedStatuses.Contains(job.Status))
            .Where(job => string.IsNullOrWhiteSpace(parameters.JobId) || job.JobId == parameters.JobId)
            .ToList();
        if (eventJobs.Count == 0)
        {
            return Preflight<JobStatusChangedResponse>();
        }

        var currentJobs = new List<JobResponse>();
        var jobService = new JobService(Client, Creds);
        foreach (var status in eventJobs.Select(job => job.Status).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            currentJobs.AddRange(await jobService.SearchJobsAsync(new SearchJobRequest { State = status }));
        }

        var currentJobsById = currentJobs
            .GroupBy(job => job.ContentId)
            .ToDictionary(group => group.Key, group => group.Last());
        var jobs = eventJobs.Select(job =>
            {
                currentJobsById.TryGetValue(job.JobId, out var currentJob);
                return new JobStatusChangedItem
                {
                    ContentId = job.JobId,
                    Name = currentJob?.Name ?? string.Empty,
                    Source = currentJob?.Source ?? job.SourceLanguage,
                    Target = currentJob?.Target ?? job.TargetLanguage,
                    Status = job.Status,
                    CreationDate = currentJob?.CreationDate ?? default,
                    PreviousStatus = job.PreviousStatus
                };
            })
            .Where(job => string.IsNullOrWhiteSpace(parameters.JobLabelContains) ||
                          job.Name.Contains(parameters.JobLabelContains, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (jobs.Count == 0)
        {
            return Preflight<JobStatusChangedResponse>();
        }

        return new WebhookResponse<JobStatusChangedResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK),
            Result = new JobStatusChangedResponse
            {
                Items = jobs,
                TotalCount = jobs.Count
            }
        };
    }

    private static WebhookResponse<T> BadRequest<T>(string message) where T : class => new()
    {
        HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(message, Encoding.UTF8, "text/plain")
        },
        Result = null,
        ReceivedWebhookRequestType = WebhookRequestType.Preflight
    };

    private static WebhookResponse<T> Preflight<T>() where T : class => new()
    {
        HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK),
        Result = null,
        ReceivedWebhookRequestType = WebhookRequestType.Preflight
    };
}
