using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Apps.Drupal.Polling.Models.Requests;
using Apps.Drupal.Webhooks;
using Apps.Drupal.Webhooks.Handlers;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tests.Drupal.Base;

namespace Tests.Drupal;

[TestClass]
public class WebhookTests : TestBase
{
    private const string CallbackUrl = "https://hooks.blackbird.io/drupal/shared-fixture";

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task SubscriptionHandlers_SharedCallback_SubscribeAndDeleteExactTopics(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var values = new Dictionary<string, string> { ["payloadUrl"] = CallbackUrl };
        var submitted = new TranslationJobsSubmittedHandler(context);
        var statusChanged = new TranslationJobStatusChangedHandler(context);

        // Act
        await submitted.SubscribeAsync(context.AuthenticationCredentialsProviders, values);
        await submitted.UnsubscribeAsync(context.AuthenticationCredentialsProviders, values);
        await statusChanged.SubscribeAsync(context.AuthenticationCredentialsProviders, values);
        await statusChanged.UnsubscribeAsync(context.AuthenticationCredentialsProviders, values);

        // Assert
        var posts = FixtureServer.Requests
            .Where(request => request.Method == "POST" && request.Path == "/api/tmgmt/blackbird/webhooks")
            .ToList();
        Assert.HasCount(2, posts);
        CollectionAssert.AreEquivalent(
            new[] { WebhookTopics.TranslationJobsSubmitted, WebhookTopics.TranslationJobStatusChanged },
            posts.Select(request => JObject.Parse(request.Body!)["events"]![0]!.Value<string>()).ToArray());
        Assert.IsTrue(posts.All(request => JObject.Parse(request.Body!)["url"]!.Value<string>() == CallbackUrl));

        var deletedPaths = FixtureServer.Requests
            .Where(request => request.Method == "DELETE")
            .Select(request => request.Path)
            .ToArray();
        CollectionAssert.AreEquivalent(
            new[]
            {
                "/api/tmgmt/blackbird/webhooks/submitted-subscription",
                "/api/tmgmt/blackbird/webhooks/status-subscription"
            },
            deletedPaths);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnTranslationJobsRequested_ValidSignatureAndTarget_OutputsEnrichedJobs(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var webhook = new WebhookList(context);
        var body = CreatePayload(WebhookTopics.TranslationJobsSubmitted, Manifest);

        // Act
        var response = await webhook.OnTranslationJobsRequested(
            CreateWebhookRequest(body),
            new TranslationJobsPollingParameters { TargetLanguages = [Manifest.ReadJob.Target] });

        // Assert
        Assert.IsNotNull(response.HttpResponseMessage);
        Assert.AreEqual(HttpStatusCode.OK, response.HttpResponseMessage.StatusCode);
        Assert.AreEqual(WebhookRequestType.Default, response.ReceivedWebhookRequestType);
        Assert.IsNotNull(response.Result);
        Assert.AreEqual(2, response.Result.TotalCount);
        CollectionAssert.AreEquivalent(
            new[] { Manifest.ReadJob.Id, Manifest.UploadJob.Id },
            response.Result.Items.Select(job => job.ContentId).ToArray());
        Assert.IsTrue(response.Result.Items.All(job => job.Status == "active"));
        Assert.IsTrue(response.Result.Items.All(job => job.CreationDate.Kind == DateTimeKind.Utc));
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnTranslationJobsRequested_ExcludedTarget_ReturnsPreflightWithoutApiCall(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var webhook = new WebhookList(context);
        var body = CreatePayload(WebhookTopics.TranslationJobsSubmitted, Manifest);

        // Act
        var response = await webhook.OnTranslationJobsRequested(
            CreateWebhookRequest(body),
            new TranslationJobsPollingParameters { TargetLanguages = ["de"] });

        // Assert
        Assert.IsNotNull(response.HttpResponseMessage);
        Assert.AreEqual(HttpStatusCode.OK, response.HttpResponseMessage.StatusCode);
        Assert.AreEqual(WebhookRequestType.Preflight, response.ReceivedWebhookRequestType);
        Assert.IsNull(response.Result);
        Assert.IsEmpty(FixtureServer.Requests);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnTranslationJobsRequested_JsonElementBody_VerifiesRawSignature(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var webhook = new WebhookList(context);
        var body = CreatePayload(WebhookTopics.TranslationJobsSubmitted, Manifest);
        using var document = JsonDocument.Parse(body);

        // Act
        var response = await webhook.OnTranslationJobsRequested(
            CreateWebhookRequest(body, requestBody: document.RootElement.Clone()),
            new TranslationJobsPollingParameters());

        // Assert
        Assert.IsNotNull(response.HttpResponseMessage);
        Assert.AreEqual(HttpStatusCode.OK, response.HttpResponseMessage.StatusCode);
        Assert.IsNotNull(response.Result);
        Assert.AreEqual(2, response.Result.TotalCount);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnJobStatusesChanged_MatchingFilters_OutputsPreviousAndCurrentStatus(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var webhook = new WebhookList(context);
        var body = CreatePayload(
            WebhookTopics.TranslationJobStatusChanged,
            Manifest,
            currentStatus: "active",
            previousStatus: "rejected",
            includeUploadJob: false);

        // Act
        var response = await webhook.OnJobStatusesChanged(
            CreateWebhookRequest(body),
            new JobStatusChangedPollingParameters
            {
                Statuses = ["active"],
                JobId = Manifest.ReadJob.Id,
                JobLabelContains = "CAPTURE READ"
            });

        // Assert
        Assert.IsNotNull(response.HttpResponseMessage);
        Assert.AreEqual(HttpStatusCode.OK, response.HttpResponseMessage.StatusCode);
        Assert.IsNotNull(response.Result);
        Assert.HasCount(1, response.Result.Items);
        Assert.AreEqual(Manifest.ReadJob.Id, response.Result.Items[0].ContentId);
        Assert.AreEqual("rejected", response.Result.Items[0].PreviousStatus);
        Assert.AreEqual("active", response.Result.Items[0].Status);
        Assert.AreEqual(Manifest.ReadJob.Name, response.Result.Items[0].Name);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnJobStatusesChanged_ExcludedStatus_ReturnsPreflightWithoutApiCall(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var webhook = new WebhookList(context);
        var body = CreatePayload(
            WebhookTopics.TranslationJobStatusChanged,
            Manifest,
            currentStatus: "rejected",
            previousStatus: "active",
            includeUploadJob: false);

        // Act
        var response = await webhook.OnJobStatusesChanged(
            CreateWebhookRequest(body),
            new JobStatusChangedPollingParameters { Statuses = ["completed"] });

        // Assert
        Assert.IsNotNull(response.HttpResponseMessage);
        Assert.AreEqual(HttpStatusCode.OK, response.HttpResponseMessage.StatusCode);
        Assert.AreEqual(WebhookRequestType.Preflight, response.ReceivedWebhookRequestType);
        Assert.IsNull(response.Result);
        Assert.IsEmpty(FixtureServer.Requests);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnTranslationJobsRequested_InvalidSignature_ReturnsBadRequestWithoutApiCall(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var webhook = new WebhookList(context);
        var body = CreatePayload(WebhookTopics.TranslationJobsSubmitted, Manifest);

        // Act
        var response = await webhook.OnTranslationJobsRequested(
            CreateWebhookRequest(body, "wrong-api-key"),
            new TranslationJobsPollingParameters());

        // Assert
        Assert.IsNotNull(response.HttpResponseMessage);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.HttpResponseMessage.StatusCode);
        Assert.AreEqual(WebhookRequestType.Preflight, response.ReceivedWebhookRequestType);
        Assert.IsNull(response.Result);
        Assert.IsEmpty(FixtureServer.Requests);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnTranslationJobsRequested_WrongTopic_ReturnsPreflightWithoutApiCall(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var webhook = new WebhookList(context);
        var body = CreatePayload(WebhookTopics.TranslationJobStatusChanged, Manifest);

        // Act
        var response = await webhook.OnTranslationJobsRequested(
            CreateWebhookRequest(body),
            new TranslationJobsPollingParameters());

        // Assert
        Assert.IsNotNull(response.HttpResponseMessage);
        Assert.AreEqual(HttpStatusCode.OK, response.HttpResponseMessage.StatusCode);
        Assert.AreEqual(WebhookRequestType.Preflight, response.ReceivedWebhookRequestType);
        Assert.IsNull(response.Result);
        Assert.IsEmpty(FixtureServer.Requests);
    }

    private static WebhookRequest CreateWebhookRequest(
        string body,
        string signingApiKey = FixtureApiServer.ValidApiKey,
        object? requestBody = null)
    {
        var signingKey = SHA256.HashData(Encoding.UTF8.GetBytes(signingApiKey));
        var signature = HMACSHA256.HashData(signingKey, Encoding.UTF8.GetBytes(body));
        return new WebhookRequest
        {
            Body = requestBody ?? body,
            Headers = new Dictionary<string, string>
            {
                ["X-Blackbird-Signature"] = $"sha256={Convert.ToHexString(signature).ToLowerInvariant()}"
            }
        };
    }

    private static string CreatePayload(
        string topic,
        CaptureManifest manifest,
        string currentStatus = "active",
        string previousStatus = "unprocessed",
        bool includeUploadJob = true)
    {
        var jobs = new[] { manifest.ReadJob, manifest.UploadJob }
            .Take(includeUploadJob ? 2 : 1)
            .Select((job, index) => new
            {
                job_id = long.Parse(job.Id),
                source_language = job.Source,
                target_language = job.Target,
                previous_status = previousStatus,
                status = currentStatus,
                items = new[]
                {
                    new
                    {
                        job_item_id = index + 1,
                        source_plugin = "content",
                        content_type = "node",
                        content_id = (index + 1).ToString()
                    }
                }
            });
        return JsonConvert.SerializeObject(new
        {
            event_id = "11111111-1111-1111-1111-111111111111",
            @event = topic,
            occurred_at = "2026-08-20T12:00:00Z",
            jobs
        });
    }
}
