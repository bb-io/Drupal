using System.Net;
using System.Text;
using Apps.Drupal.Api;
using Apps.Drupal.Constants;
using Apps.Drupal.Invocables;
using Apps.Drupal.Models.Identifiers;
using Apps.Drupal.Models.Requests;
using Apps.Drupal.Models.Responses;
using Apps.Drupal.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using HtmlAgilityPack;
using RestSharp;

namespace Apps.Drupal.Actions;

[ActionList("Jobs")]
public class JobActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AppInvocable(invocationContext)
{
    [Action("Search jobs", Description = "Search translation jobs using optional filters")]
    [BlueprintActionDefinition(BlueprintAction.SearchContent)]
    public async Task<JobSearchResponse> SearchJobsAsync([ActionParameter] SearchJobRequest filterRequest)
    {
        var jobs = await new JobService(Client, Creds).SearchJobsAsync(filterRequest);

        return new JobSearchResponse
        {
            Items = jobs,
            TotalCount = jobs.Count
        };
    }

    [Action("Accept job", Description = "Accept an unprocessed translation job for processing")]
    public async Task<AcceptJobResponse> AcceptJobAsync([ActionParameter] AcceptJobRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.JobId))
        {
            throw new PluginMisconfigurationException("Job ID is required.");
        }

        var request = new ApiRequest($"/api/tmgmt/blackbird/job/{input.JobId}/accept", Method.Post, Creds);
        return await Client.ExecuteWithErrorHandling<AcceptJobResponse>(request)
            ?? throw new PluginApplicationException("Drupal returned an empty job-acceptance response.");
    }

    [Action("Reject job", Description = "Reject an active translation job and record the reason in Drupal")]
    public async Task<RejectJobResponse> RejectJobAsync([ActionParameter] RejectJobRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.JobId))
        {
            throw new PluginMisconfigurationException("Job ID is required.");
        }

        if (string.IsNullOrWhiteSpace(input.RejectionReason))
        {
            throw new PluginMisconfigurationException("Rejection reason is required.");
        }

        var request = new ApiRequest($"/api/tmgmt/blackbird/job/{input.JobId}/reject", Method.Post, Creds)
            .AddJsonBody(new { reason = input.RejectionReason.Trim() });

        return await Client.ExecuteWithErrorHandling<RejectJobResponse>(request)
            ?? throw new PluginApplicationException("Drupal returned an empty job-rejection response.");
    }

    [Action("Download job content", Description = "Download assembled content from a translation job")]
    [BlueprintActionDefinition(BlueprintAction.DownloadContent)]
    public async Task<GetXliffFromJobResponse> GetXliffFromJobAsync([ActionParameter] JobIdentifier identifier)
    {
        var jobService = new JobService(Client, Creds);
        JobResponse? job = null;
        foreach (var state in new[] { "unprocessed", "active", "completed", "aborted" })
        {
            job = (await jobService.SearchJobsAsync(new SearchJobRequest { State = state }))
                .FirstOrDefault(x => x.ContentId == identifier.ContentId);
            if (job is not null)
            {
                break;
            }
        }

        if (job is null)
        {
            throw new PluginMisconfigurationException($"Translation job '{identifier.ContentId}' was not found.");
        }

        var apiRequest = new ApiRequest($"/api/tmgmt/blackbird/job/{identifier.ContentId}", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling(apiRequest);
        var rawHtml = response.Content
            ?? throw new PluginApplicationException("Drupal returned empty job content.");

        if (identifier.FileFormat == "original")
        {
            var originalStream = new MemoryStream(Encoding.UTF8.GetBytes(rawHtml));
            var result = new GetXliffFromJobResponse
            {
                Content = await fileManagementClient.UploadAsync(
                    originalStream,
                    "text/html",
                    $"{identifier.ContentId}.html")
            };
            if (identifier.AcceptJob ?? true)
            {
                await AcceptJobAsync(new AcceptJobRequest { JobId = identifier.ContentId });
            }

            return result;
        }

        if (identifier.FileFormat is not null and not "text/html")
        {
            throw new PluginMisconfigurationException($"Unsupported file format: {identifier.FileFormat}");
        }

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(rawHtml);

        var htmlNode = htmlDocument.DocumentNode.SelectSingleNode("//html")
            ?? throw new PluginApplicationException("Drupal job content does not contain an HTML element.");
        var headNode = htmlNode.SelectSingleNode("head")
            ?? throw new PluginApplicationException("Drupal job content does not contain a head element.");
        var bodyNode = htmlNode.SelectSingleNode("body")
            ?? throw new PluginApplicationException("Drupal job content does not contain a body element.");
        var atomNodes = htmlDocument.DocumentNode.SelectNodes(
            "//div[contains(concat(' ', normalize-space(@class), ' '), ' atom ')]");
        if (atomNodes is null || atomNodes.Count == 0)
        {
            throw new PluginApplicationException("Drupal job content does not contain translatable atoms.");
        }

        htmlNode.SetAttributeValue("lang", job.Source);
        htmlNode.SetAttributeValue("xml:lang", job.Source);
        headNode.SelectSingleNode("title")?.Remove();

        var baseUrl = Creds.Get(CredsNames.BaseUrl).Value.TrimEnd('/');
        foreach (var (name, value) in new[]
                 {
                     ("blackbird-ucid", job.ContentId),
                     ("blackbird-content-name", job.Name),
                     ("blackbird-admin-url", $"{baseUrl}/admin/tmgmt/jobs/{job.ContentId}"),
                     ("blackbird-system-name", "Drupal"),
                     ("blackbird-system-ref", baseUrl)
                 })
        {
            var metadataNode = htmlDocument.CreateElement("meta");
            metadataNode.SetAttributeValue("name", name);
            metadataNode.SetAttributeValue("content", value);
            headNode.AppendChild(metadataNode);
        }

        bodyNode.SetAttributeValue("its-rev-tool", "Drupal");
        bodyNode.SetAttributeValue("its-rev-tool-ref", baseUrl);

        foreach (var atomNode in atomNodes)
        {
            var assetNode = atomNode.Ancestors("div").FirstOrDefault(x =>
                x.GetAttributeValue("class", string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Contains("asset"));
            var assetId = assetNode?.GetAttributeValue("id", "asset") ?? "asset";
            var atomId = atomNode.GetAttributeValue("id", string.Empty);
            if (string.IsNullOrWhiteSpace(atomId))
            {
                throw new PluginApplicationException("Drupal job contains an atom without an ID.");
            }

            atomNode.SetAttributeValue("data-blackbird-key", $"{job.ContentId}-{assetId}-{atomId}");
            atomNode.InnerHtml = WebUtility.HtmlDecode(atomNode.InnerHtml);
        }

        var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(htmlDocument.DocumentNode.OuterHtml));
        var output = new GetXliffFromJobResponse
        {
            Content = await fileManagementClient.UploadAsync(
                outputStream,
                "text/html",
                $"{identifier.ContentId}.html")
        };
        if (identifier.AcceptJob ?? true)
        {
            await AcceptJobAsync(new AcceptJobRequest { JobId = identifier.ContentId });
        }

        return output;
    }

    [Action("Upload job content", Description = "Upload translated content to a translation job and output content with updated metadata")]
    [BlueprintActionDefinition(BlueprintAction.UploadContent)]
    public async Task<GetXliffFromJobResponse> TranslateJobAsync([ActionParameter] TranslateJobRequest input)
    {
        var extension = Path.GetExtension(input.Content.Name);
        var isHtml = extension.Equals(".html", StringComparison.OrdinalIgnoreCase);
        if (!isHtml &&
            !extension.Equals(".xlf", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".xliff", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                "Unsupported or invalid content file. Only .html, .xlf, and .xliff files are supported.");
        }

        var inputStream = await fileManagementClient.DownloadAsync(input.Content);
        var inputBytes = await inputStream.GetByteData();
        var transformationResult = Transformation.Load(
            new MemoryStream(inputBytes),
            input.Content.Name,
            input.Content.ContentType);

        string targetHtml;
        if (transformationResult.Success)
        {
            var targetResult = transformationResult.Target();
            if (targetResult.Success)
            {
                targetHtml = targetResult.Value.ToStream().ReadString();
            }
            else if (isHtml)
            {
                targetHtml = Encoding.UTF8.GetString(inputBytes);
            }
            else
            {
                throw new PluginMisconfigurationException(
                    $"Could not read translated content: {targetResult.Error}");
            }
        }
        else if (isHtml)
        {
            targetHtml = Encoding.UTF8.GetString(inputBytes);
        }
        else
        {
            throw new PluginMisconfigurationException(
                $"Unsupported or invalid content file: {transformationResult.Error}");
        }

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(targetHtml);

        var standardJobId = htmlDocument.DocumentNode
            .SelectSingleNode("//meta[@name='blackbird-ucid']")?
            .GetAttributeValue("content", string.Empty);
        var legacyJobId = htmlDocument.DocumentNode
            .SelectSingleNode("//meta[@name='JobID']")?
            .GetAttributeValue("content", string.Empty);
        if (!string.IsNullOrWhiteSpace(standardJobId) &&
            !string.IsNullOrWhiteSpace(legacyJobId) &&
            standardJobId != legacyJobId)
        {
            throw new PluginMisconfigurationException(
                $"Content Job ID '{standardJobId}' does not match legacy Job ID '{legacyJobId}'.");
        }

        var embeddedJobId = !string.IsNullOrWhiteSpace(standardJobId) ? standardJobId : legacyJobId;
        if (string.IsNullOrWhiteSpace(input.ContentId) && string.IsNullOrWhiteSpace(embeddedJobId))
        {
            throw new PluginMisconfigurationException(
                "Job ID is missing. Provide Job ID or use content downloaded from Drupal.");
        }

        if (!string.IsNullOrWhiteSpace(input.ContentId) &&
            !string.IsNullOrWhiteSpace(embeddedJobId) &&
            input.ContentId != embeddedJobId)
        {
            throw new PluginMisconfigurationException(
                $"Provided Job ID '{input.ContentId}' does not match content Job ID '{embeddedJobId}'.");
        }

        var jobId = input.ContentId ?? embeddedJobId!;
        var jobService = new JobService(Client, Creds);
        JobResponse? job = null;
        foreach (var state in new[] { "active", "unprocessed", "completed", "aborted" })
        {
            job = (await jobService.SearchJobsAsync(new SearchJobRequest { State = state }))
                .FirstOrDefault(x => x.ContentId == jobId);
            if (job is not null)
            {
                break;
            }
        }

        if (job is null)
        {
            throw new PluginMisconfigurationException($"Translation job '{jobId}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(input.Locale))
        {
            throw new PluginMisconfigurationException("Target locale is required.");
        }

        if (!string.Equals(input.Locale, job.Target, StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                $"Target locale '{input.Locale}' does not match job target locale '{job.Target}'.");
        }

        var embeddedTargetLocale = htmlDocument.DocumentNode
            .SelectSingleNode("//meta[@name='languageTarget']")?
            .GetAttributeValue("content", string.Empty);
        if (!string.IsNullOrWhiteSpace(embeddedTargetLocale) &&
            !string.Equals(input.Locale, embeddedTargetLocale, StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                $"Target locale '{input.Locale}' does not match content target locale '{embeddedTargetLocale}'.");
        }

        var atomNodes = htmlDocument.DocumentNode.SelectNodes(
            "//div[contains(concat(' ', normalize-space(@class), ' '), ' atom ')]");
        if (atomNodes is null || atomNodes.Count == 0)
        {
            throw new PluginMisconfigurationException("Translated content does not contain Drupal job atoms.");
        }

        foreach (var atomNode in atomNodes)
        {
            atomNode.InnerHtml = WebUtility.HtmlEncode(atomNode.InnerHtml);
        }

        var postRequest = new ApiRequest($"/api/tmgmt/blackbird/job/{jobId}", Method.Post, Creds)
            .AddHeader("Content-Type", "text/html")
            .AddParameter("text/html", htmlDocument.DocumentNode.OuterHtml, ParameterType.RequestBody);
        await Client.ExecuteWithErrorHandling(postRequest);

        if (!transformationResult.Success)
        {
            return new GetXliffFromJobResponse { Content = input.Content };
        }

        var baseUrl = Creds.Get(CredsNames.BaseUrl).Value.TrimEnd('/');
        var transformation = transformationResult.Value;
        transformation.TargetSystemReference.ContentId = job.ContentId;
        transformation.TargetSystemReference.ContentName = job.Name;
        transformation.TargetSystemReference.AdminUrl = $"{baseUrl}/admin/tmgmt/jobs/{job.ContentId}";
        transformation.TargetSystemReference.SystemName = "Drupal";
        transformation.TargetSystemReference.SystemRef = baseUrl;
        transformation.TargetLanguage = input.Locale;

        if (transformationResult.WasBilingual)
        {
            return new GetXliffFromJobResponse
            {
                Content = await fileManagementClient.UploadAsync(
                    transformation.ToStream(),
                    "application/xliff+xml",
                    transformation.BilingualFileName)
            };
        }

        var outputTargetResult = transformation.Target();
        if (!outputTargetResult.Success)
        {
            return new GetXliffFromJobResponse { Content = input.Content };
        }

        var target = outputTargetResult.Value;
        target.SystemReference = transformation.TargetSystemReference;
        target.Language = input.Locale;
        return new GetXliffFromJobResponse
        {
            Content = await fileManagementClient.UploadAsync(
                target.ToStream(),
                target.OriginalMediaType,
                target.OriginalName)
        };
    }
}
