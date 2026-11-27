using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using Apps.Drupal.Api;
using Apps.Drupal.Invocables;
using Apps.Drupal.Models.Dtos;
using Apps.Drupal.Models.Identifiers;
using Apps.Drupal.Models.Requests;
using Apps.Drupal.Models.Responses;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Shared;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Drupal.Actions;

[ActionList("Sources (Beta)")]
public class SourceActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AppInvocable(invocationContext)
{
    private const string SourceEndpoint = "/api/tmgmt/blackbird/source";

    [Action("Get source node (Beta)", Description = "Get metadata for the default revision of a node by ID")]
    public async Task<SourceNodeResponse> GetSourceNodeAsync([ActionParameter] SourceNodeIdentifier input)
    {
        var nodeId = input.NodeId?.TrimStart('0');

        if (string.IsNullOrEmpty(nodeId) || !nodeId.All(char.IsAsciiDigit))
            throw new PluginMisconfigurationException("A positive numeric node ID is required.");

        var request = new ApiRequest($"{SourceEndpoint}/node/{nodeId}", Method.Get, Creds);

        var node = await Client.ExecuteWithErrorHandling<SourceNodeDto>(request)
            ?? throw new PluginApplicationException("Drupal returned an empty source-node response.");

        return new SourceNodeResponse
        {
            NodeId = node.NodeId,
            Uuid = node.Uuid,
            Title = node.Title,
            ContentType = node.ContentType,
            SourceLanguage = node.SourceLanguage,
            AvailableLanguages = node.AvailableLanguages,
            Published = node.Published,
            AuthorId = node.AuthorId,
            CreationDate = node.CreationDate,
            LastModifiedDate = node.LastModifiedDate,
            RevisionId = node.RevisionId,
            SourceUrl = node.SourceUrl,
            AdminUrl = node.AdminUrl
        };
    }

    [Action("Download source content (Beta)", Description = "Download a node for translation or collect an existing language for translation memory")]
    public async Task<DownloadSourceContentResponse> DownloadSourceContentAsync(
        [ActionParameter] SourceNodeIdentifier identifier,
        [ActionParameter] DownloadSourceContentRequest input)
    {
        var request = new ApiRequest(SourceEndpoint, Method.Get, Creds)
            .AddQueryParameter("id", identifier.NodeId);

        if (!string.IsNullOrWhiteSpace(input.SourceLanguage))
            request.AddQueryParameter("source", input.SourceLanguage);

        var response = await Client.ExecuteWithErrorHandling(request);
        if (response.RawBytes is not { Length: > 0 } html)
            throw new PluginApplicationException("Drupal returned empty source content.");

        var dispositionHeader = response
            .ContentHeaders?
            .FirstOrDefault(header => string.Equals(header.Name, "Content-Disposition", StringComparison.OrdinalIgnoreCase))?
            .Value?
            .ToString();

        var disposition = ContentDispositionHeaderValue.TryParse(dispositionHeader, out var parsed)
            ? parsed
            : null;

        var fileName = disposition?.FileNameStar;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = disposition?.FileName;
            if (fileName is ['"', .., '"'])
                fileName = fileName[1..^1];
        }

        if (string.IsNullOrWhiteSpace(fileName))
            throw new PluginApplicationException("Drupal returned source content without a valid download filename.");

        using var stream = new MemoryStream(html, writable: false);
        var content = await fileManagementClient.UploadAsync(stream, "text/html", fileName);
        return new DownloadSourceContentResponse { Content = content };
    }

    [Action("Upload source content (Beta)", Description = "Save translated content in the selected target language and return the submitted file with target metadata when available")]
    public async Task<UploadSourceContentResponse> UploadSourceContentAsync(
        [ActionParameter] UploadSourceContentRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.TargetLanguage))
            throw new PluginMisconfigurationException("A target language is required.");

        await using var stream = await fileManagementClient.DownloadAsync(input.Content);

        var loaded = Transformation.Load(stream, input.Content.Name, input.Content.ContentType);
        if (!loaded.Success)
            throw new PluginMisconfigurationException(loaded.Error);

        var transformation = loaded.Value;
        var rendered = transformation.Target();
        if (!rendered.Success)
            throw new PluginMisconfigurationException(rendered.Error);

        var targetHtml = rendered.Value;
        if (targetHtml.OriginalMediaType != MediaTypeNames.Text.Html)
            throw new PluginMisconfigurationException("Expected HTML generated by Blackbird.");

        // Drupal reads the source locale and node identity from the embedded HTML.
        targetHtml.Language = null;
        var request = new ApiRequest(SourceEndpoint, Method.Post, Creds) { AlwaysSingleFileAsContent = true };
        
        request
            .AddQueryParameter("target", input.TargetLanguage)
            .AddFile("content", () => targetHtml.ToStream(MetadataHandling.Exclude), "translation.html", MediaTypeNames.Text.Html);

        if (input.Publish is bool publish)
            request.AddQueryParameter("publish", publish ? "true" : "false");

        var result = await ExecuteSourceUploadAsync(request)
            ?? throw new PluginApplicationException("Drupal returned an empty source upload response.");

        var response = new UploadSourceContentResponse
        {
            SourcePlugin = result.SourcePlugin,
            ItemType = result.ItemType,
            ItemId = result.ItemId,
            SourceLanguage = result.SourceLanguage,
            TargetLanguage = result.TargetLanguage,
            Outcome = result.Outcome,
            RevisionId = result.RevisionId,
            DefaultRevision = result.DefaultRevision,
            SkippedFields = result.SkippedFields,
            Warnings = result.Warnings
        };

        foreach (var warning in response.Warnings)
            InvocationContext.Logger?.LogWarning?.Invoke(warning, []);

        if (result.TargetMetadata is not { } metadata)
            return response;

        transformation.TargetLanguage = response.TargetLanguage;
        transformation.TargetSystemReference = metadata;

        var outputName = "saved-" + Path.GetFileName(input.Content.Name);

        if (loaded.WasBilingual)
        {
            transformation.SourceSystemReference.ContentId ??= metadata.ContentId;
            using var output = transformation.ToStream();
            response.Content = await fileManagementClient.UploadAsync(output, MediaTypes.Xliff2, Path.ChangeExtension(outputName, ".xlf"));
        }
        else
        {
            targetHtml.Language = transformation.TargetLanguage;
            targetHtml.SystemReference = new SystemReference
            {
                ContentId = WebUtility.HtmlEncode(metadata.ContentId),
                ContentName = WebUtility.HtmlEncode(metadata.ContentName ?? string.Empty),
                AdminUrl = WebUtility.HtmlEncode(metadata.AdminUrl ?? string.Empty),
                PublicUrl = WebUtility.HtmlEncode(metadata.PublicUrl ?? string.Empty),
                SystemName = WebUtility.HtmlEncode(metadata.SystemName),
                SystemRef = WebUtility.HtmlEncode(metadata.SystemRef),
            };
            using var output = targetHtml.ToStream();
            response.Content = await fileManagementClient.UploadAsync(output, MediaTypeNames.Text.Html, outputName);
        }

        return response;
    }

    private async Task<UploadSourceContentDto?> ExecuteSourceUploadAsync(RestRequest request)
    {
        try
        {
            var response = await Client.ExecuteWithErrorHandling(request);
            return response.Content is null ? null : JsonConvert.DeserializeObject<UploadSourceContentDto>(response.Content);
        }
        catch (PluginApplicationException error)
        {
            if (error.Message.StartsWith("Status code: 504", StringComparison.Ordinal))
                throw new PluginApplicationException("Status code: 504. Drupal took too long to reply but may have saved your translation.");

            foreach (var (prefix, expectedCode) in new[]
            {
                ("Status code: 400, Error: ", "duplicate_field"),
                ("Status code: 422, Error: ", "no_applicable_fields")
            })
            {
                if (!error.Message.StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                var content = error.Message[prefix.Length..];
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                try
                {
                    using var body = JsonDocument.Parse(content);
                    var uploadError = body.RootElement;
                    if (uploadError.ValueKind != JsonValueKind.Object ||
                        !uploadError.TryGetProperty("code", out var code) || code.ValueKind != JsonValueKind.String ||
                        !code.ValueEquals(expectedCode) ||
                        !uploadError.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.String)
                        continue;

                    if (uploadError.TryGetProperty("warnings", out var warnings) && warnings.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var warning in warnings.EnumerateArray())
                        {
                            if (warning.ValueKind == JsonValueKind.String && warning.GetString() is { Length: > 0 } text)
                                InvocationContext.Logger?.LogWarning?.Invoke(text, []);
                        }
                    }

                    if (message.GetString() is { } textMessage)
                        throw new PluginApplicationException(prefix + textMessage);
                }
                catch (System.Text.Json.JsonException)
                {
                }
            }

            throw;
        }
    }
}
