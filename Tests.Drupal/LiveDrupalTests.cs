using System.Text;
using Apps.Drupal.Actions;
using Apps.Drupal.Connections;
using Apps.Drupal.DataSources;
using Apps.Drupal.Models.Identifiers;
using Apps.Drupal.Models.Requests;
using Apps.Drupal.Models.Responses;
using Apps.Drupal.Polling;
using Apps.Drupal.Polling.Models;
using Apps.Drupal.Polling.Models.Requests;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Polling;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using HtmlAgilityPack;
using Tests.Drupal.Base;

namespace Tests.Drupal;

[TestClass]
[DoNotParallelize]
public class LiveDrupalTests : TestBase
{
    [TestMethod]
    [LiveContextDataSource]
    public async Task ValidateConnection_LiveDemo_ReturnsValid(LiveContext liveContext)
    {
        // Arrange
        var context = CreateLiveInvocationContext(liveContext);
        var validator = new ConnectionValidator();

        // Act
        var result = await validator.ValidateConnection(
            context.AuthenticationCredentialsProviders,
            CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    [LiveContextDataSource]
    public async Task SearchJobsAsync_LiveDemoFilters_ReturnsOwnedReadAndUploadJobs(LiveContext liveContext)
    {
        // Arrange
        var context = CreateLiveInvocationContext(liveContext);
        var actions = new JobActions(context, Files);
        var jobs = await GetOwnedJobs(actions, liveContext.Version);
        var createdAfter = jobs.Min(job => job.CreationDate).AddSeconds(-1);

        // Act
        var result = await actions.SearchJobsAsync(new SearchJobRequest
        {
            TargetLanguage = "fr",
            CreatedAfter = createdAfter
        });

        // Assert
        Assert.IsTrue(result.Items.Any(job => job.Name == ReadJobName(liveContext.Version)));
        Assert.IsTrue(result.Items.Any(job => job.Name == UploadJobName(liveContext.Version)));
        Assert.IsTrue(result.Items.All(job => job.Target == "fr"));
        Assert.IsTrue(result.Items.All(job => job.CreationDate >= createdAfter));
        Assert.IsTrue(result.Items.All(job => job.Status == "unprocessed"));
    }

    [TestMethod]
    [LiveContextDataSource]
    public async Task DataHandlers_LiveDemo_ReturnValuesAndSearchFiltering(LiveContext liveContext)
    {
        // Arrange
        var context = CreateLiveInvocationContext(liveContext);
        var languageHandler = new LanguagesDataHandler(context);
        var jobHandler = new JobDataHandler(context);

        // Act
        var languages = (await languageHandler.GetDataAsync(
            new DataSourceContext { SearchString = "French" }, CancellationToken.None)).ToList();
        var jobs = (await jobHandler.GetDataAsync(
            new DataSourceContext { SearchString = "capture upload" }, CancellationToken.None)).ToList();

        // Assert
        Assert.IsTrue(languages.Any(item => item.Value == "fr" && item.DisplayName == "French"));
        Assert.HasCount(1, jobs);
        Assert.AreEqual(UploadJobName(liveContext.Version), jobs[0].DisplayName);
    }

    [TestMethod]
    [LiveContextDataSource]
    public async Task OnTranslationJobRequested_LiveDemoMatchingLanguage_ReturnsOwnedJobs(LiveContext liveContext)
    {
        // Arrange
        var context = CreateLiveInvocationContext(liveContext);
        var actions = new JobActions(context, Files);
        var jobs = await GetOwnedJobs(actions, liveContext.Version);
        var memory = jobs.Min(job => job.CreationDate).AddSeconds(-1);

        // Act
        var result = await new PollingList(context).OnTranslationJobRequested(
            new PollingEventRequest<DateMemory>
            {
                Memory = new DateMemory { LastPollingTime = memory }
            },
            new TranslationJobsPollingParameters { TargetLanguages = ["fr"] });
        var statusResult = await new PollingList(context).OnJobStatusChanged(
            new PollingEventRequest<JobStatusMemory>
            {
                Memory = new JobStatusMemory
                {
                    LastPollingTime = memory,
                    JobStatuses = new Dictionary<string, string>
                    {
                        [jobs.Single(job => job.Name == UploadJobName(liveContext.Version)).ContentId] = "unprocessed"
                    }
                }
            },
            new JobStatusChangedPollingParameters
            {
                Statuses = ["unprocessed"],
                JobId = jobs.Single(job => job.Name == ReadJobName(liveContext.Version)).ContentId
            });

        // Assert
        Assert.IsTrue(result.FlyBird);
        Assert.IsNotNull(result.Result);
        Assert.IsTrue(result.Result.Items.Any(job => job.Name == ReadJobName(liveContext.Version)));
        Assert.IsTrue(result.Result.Items.Any(job => job.Name == UploadJobName(liveContext.Version)));
        Assert.AreEqual(DateTimeKind.Utc, result.Memory!.LastPollingTime.Kind);
        Assert.IsTrue(statusResult.FlyBird);
        Assert.IsNotNull(statusResult.Result);
        Assert.HasCount(1, statusResult.Result.Items);
        Assert.AreEqual("unprocessed", statusResult.Result.Items[0].Status);
        Assert.AreEqual(string.Empty, statusResult.Result.Items[0].PreviousStatus);
    }

    [TestMethod]
    public async Task RejectJobAndStatusChanged_Drupal11Live_RejectsJobAndTriggers()
    {
        // Arrange
        var liveContext = LoadLiveContexts().Single(context => context.Version == 11);
        var context = CreateLiveInvocationContext(liveContext);
        var actions = new JobActions(context, Files);
        const string jobLabel = "Blackbird connector status transition Drupal 11";
        var job = (await actions.SearchJobsAsync(new SearchJobRequest { State = "active" }))
            .Items.SingleOrDefault(item => item.Name == jobLabel);
        if (job is null)
        {
            job = (await actions.SearchJobsAsync(new SearchJobRequest { State = "unprocessed" }))
                .Items.SingleOrDefault(item => item.Name == jobLabel);
            if (job is null)
            {
                Assert.Inconclusive("Drupal 11 status-transition job was not found. Run prepare-demo.sh first.");
            }

            var acceptResponse = await actions.AcceptJobAsync(new AcceptJobRequest { JobId = job.ContentId });
            Assert.AreEqual(job.ContentId, acceptResponse.JobId);
            Assert.AreEqual("active", acceptResponse.Status);
        }

        var polling = new PollingList(context);
        var baseline = await polling.OnJobStatusChanged(
            new PollingEventRequest<JobStatusMemory>(),
            new JobStatusChangedPollingParameters
            {
                Statuses = ["rejected"],
                JobId = job.ContentId,
                JobLabelContains = "STATUS TRANSITION"
            });

        // Act
        var actionResult = await actions.RejectJobAsync(new RejectJobRequest
        {
            JobId = job.ContentId,
            RejectionReason = "Blackbird connector live-test failure"
        });
        var repeatedActionResult = await actions.RejectJobAsync(new RejectJobRequest
        {
            JobId = job.ContentId,
            RejectionReason = "Blackbird connector live-test failure"
        });
        var eventResult = await polling.OnJobStatusChanged(
            new PollingEventRequest<JobStatusMemory> { Memory = baseline.Memory },
            new JobStatusChangedPollingParameters
            {
                Statuses = ["rejected"],
                JobId = job.ContentId,
                JobLabelContains = "status transition"
            });

        // Assert
        Assert.IsFalse(baseline.FlyBird);
        Assert.IsNull(baseline.Result);
        Assert.AreEqual(job.ContentId, actionResult.JobId);
        Assert.AreEqual("rejected", actionResult.Status);
        Assert.AreEqual(job.ContentId, repeatedActionResult.JobId);
        Assert.AreEqual("rejected", repeatedActionResult.Status);
        Assert.IsTrue(eventResult.FlyBird);
        Assert.IsNotNull(eventResult.Result);
        Assert.HasCount(1, eventResult.Result.Items);
        Assert.AreEqual("active", eventResult.Result.Items[0].PreviousStatus);
        Assert.AreEqual("rejected", eventResult.Result.Items[0].Status);
        Assert.AreEqual(job.ContentId, eventResult.Result.Items[0].ContentId);
    }

    [TestMethod]
    public async Task OnTranslationJobRequested_Drupal11BulkLanguageFanOut_ReturnsBothTargets()
    {
        // Arrange
        var liveContext = LoadLiveContexts().Single(context => context.Version == 11);
        var context = CreateLiveInvocationContext(liveContext);
        var actions = new JobActions(context, Files);
        var bulkJobs = (await actions.SearchJobsAsync(new SearchJobRequest())).Items
            .Where(job => job.Name.StartsWith("Bulk connector verification", StringComparison.Ordinal))
            .Where(job => job.Target is "de" or "fr")
            .ToList();
        if (!new[] { "de", "fr" }.All(target => bulkJobs.Any(job => job.Target == target)))
        {
            Assert.Inconclusive("Unprocessed Drupal 11 French/German bulk jobs were not found. Run bulk UI setup first.");
        }

        // Act
        var result = await new PollingList(context).OnTranslationJobRequested(
            new PollingEventRequest<DateMemory>
            {
                Memory = new DateMemory
                {
                    LastPollingTime = bulkJobs.Min(job => job.CreationDate).AddSeconds(-1)
                }
            },
            new TranslationJobsPollingParameters { TargetLanguages = ["de", "fr"] });

        // Assert
        Assert.IsTrue(result.FlyBird);
        Assert.IsNotNull(result.Result);
        var returnedBulkJobs = result.Result.Items
            .Where(job => job.Name.StartsWith("Bulk connector verification", StringComparison.Ordinal))
            .ToList();
        CollectionAssert.AreEquivalent(
            new[] { "de", "fr" },
            returnedBulkJobs.Select(job => job.Target).Distinct().ToArray());
        Assert.IsTrue(returnedBulkJobs.All(job => bulkJobs.Any(expected => expected.ContentId == job.ContentId)));
    }

    [TestMethod]
    [LiveContextDataSource]
    public async Task GetXliffFromJobAsync_LiveReadJob_ReturnsOriginalAndDecoratedContent(LiveContext liveContext)
    {
        // Arrange
        var context = CreateLiveInvocationContext(liveContext);
        var actions = new JobActions(context, Files);
        var job = (await GetOwnedJobs(actions, liveContext.Version))
            .Single(item => item.Name == ReadJobName(liveContext.Version));

        // Act
        var original = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = job.ContentId,
            FileFormat = "original"
        });
        var originalHtml = Files.ReadOutputText(original.Content);
        var downloadNote = $"Blackbird download test Drupal {liveContext.Version}";
        var decorated = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = job.ContentId,
            Note = downloadNote
        });
        var noteAfterDownload = await actions.GetJobNoteAsync(new JobNoteIdentifier { JobId = job.ContentId });
        var updatedNote = $"Blackbird updated note Drupal {liveContext.Version}";
        var setNote = await actions.SetJobNoteAsync(new SetJobNoteRequest
        {
            JobId = job.ContentId,
            Note = updatedNote
        });
        var noteAfterSet = await actions.GetJobNoteAsync(new JobNoteIdentifier { JobId = job.ContentId });

        // Assert
        Assert.IsFalse(originalHtml.Contains("blackbird-ucid", StringComparison.Ordinal));
        AssertDecoratedHtml(Files.ReadOutputText(decorated.Content), job, liveContext.BaseUrl.TrimEnd('/'));
        Assert.AreEqual(job.ContentId, noteAfterDownload.JobId);
        Assert.AreEqual(downloadNote, noteAfterDownload.Note);
        Assert.AreEqual(updatedNote, setNote.Note);
        Assert.AreEqual(updatedNote, noteAfterSet.Note);
    }

    [TestMethod]
    [LiveContextDataSource]
    public async Task TranslateJobAsync_LiveValidationFailures_RejectWrongLocaleAndConflictingId(LiveContext liveContext)
    {
        // Arrange
        var context = CreateLiveInvocationContext(liveContext);
        var actions = new JobActions(context, Files);
        var jobs = await GetOwnedJobs(actions, liveContext.Version);
        var readJob = jobs.Single(job => job.Name == ReadJobName(liveContext.Version));
        var uploadJob = jobs.Single(job => job.Name == UploadJobName(liveContext.Version));
        var download = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = readJob.ContentId
        });

        // Act
        var wrongLocale = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            actions.TranslateJobAsync(new TranslateJobRequest
            {
                Content = download.Content,
                Locale = "de"
            }));
        var conflictingId = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            actions.TranslateJobAsync(new TranslateJobRequest
            {
                Content = download.Content,
                ContentId = uploadJob.ContentId,
                Locale = readJob.Target
            }));

        // Assert
        StringAssert.Contains(wrongLocale.Message, "does not match job target locale");
        StringAssert.Contains(conflictingId.Message, "does not match content Job ID");
    }

    [TestMethod]
    [LiveContextDataSource]
    public async Task TranslateJobAsync_LiveUploadJob_RoundtripsHtmlXliff1AndXliff2(LiveContext liveContext)
    {
        // Arrange
        var context = CreateLiveInvocationContext(liveContext);
        var actions = new JobActions(context, Files);
        var job = (await GetOwnedJobs(actions, liveContext.Version))
            .Single(item => item.Name == UploadJobName(liveContext.Version));
        var download = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = job.ContentId
        });
        var downloadedBytes = Files.ReadOutput(download.Content);

        // Act
        var htmlResult = await actions.TranslateJobAsync(new TranslateJobRequest
        {
            Content = download.Content,
            Locale = job.Target
        });
        var xliff1Result = await UploadXliff(actions, download, downloadedBytes, job, liveContext.Version, 1);
        var xliff2Result = await UploadXliff(actions, download, downloadedBytes, job, liveContext.Version, 2);

        // Assert
        var htmlDocument = LoadHtml(Files.ReadOutputText(htmlResult.Content));
        Assert.AreEqual(job.Target,
            htmlDocument.DocumentNode.SelectSingleNode("//html")!.GetAttributeValue("lang", string.Empty));
        AssertMetadata(htmlDocument, "blackbird-ucid", job.ContentId);
        AssertReturnedXliff(xliff1Result, job);
        AssertReturnedXliff(xliff2Result, job);
    }

    private async Task<GetXliffFromJobResponse> UploadXliff(
        JobActions actions,
        GetXliffFromJobResponse download,
        byte[] downloadedBytes,
        JobResponse job,
        int version,
        int xliffVersion)
    {
        var transformation = Transformation.Load(
            new MemoryStream(downloadedBytes),
            download.Content.Name,
            download.Content.ContentType).Value
            ?? throw new AssertFailedException("Downloaded HTML could not be transformed.");
        transformation.TargetLanguage = job.Target;
        foreach (var segment in transformation.GetUnits().SelectMany(unit => unit.Segments))
        {
            segment.SetTarget(segment.GetSource());
            segment.State = SegmentState.Translated;
        }

        var content = Files.WriteOutput(
            $"drupal-{version}-live-xliff{xliffVersion}.xlf",
            xliffVersion == 1
                ? Encoding.UTF8.GetBytes(Xliff1Serializer.Serialize(transformation))
                : Encoding.UTF8.GetBytes(transformation.Serialize()),
            xliffVersion == 1 ? "application/x-xliff+xml" : "application/xliff+xml");

        return await actions.TranslateJobAsync(new TranslateJobRequest
        {
            Content = content,
            ContentId = job.ContentId,
            Locale = job.Target
        });
    }

    private void AssertReturnedXliff(GetXliffFromJobResponse response, JobResponse job)
    {
        Assert.AreEqual("application/xliff+xml", response.Content.ContentType);
        var transformation = Transformation.Load(
            new MemoryStream(Files.ReadOutput(response.Content)),
            response.Content.Name,
            response.Content.ContentType).Value
            ?? throw new AssertFailedException("Returned XLIFF could not be transformed.");
        Assert.AreEqual(job.ContentId, transformation.TargetSystemReference.ContentId);
        Assert.AreEqual(job.Target, transformation.TargetLanguage);
    }

    private static async Task<List<JobResponse>> GetOwnedJobs(JobActions actions, int version)
    {
        var unprocessed = await actions.SearchJobsAsync(new SearchJobRequest());
        var active = await actions.SearchJobsAsync(new SearchJobRequest { State = "active" });
        var names = new[] { ReadJobName(version), UploadJobName(version) };
        var jobs = unprocessed.Items.Concat(active.Items)
            .Where(job => names.Contains(job.Name))
            .GroupBy(job => job.ContentId)
            .Select(group => group.Last())
            .ToList();
        Assert.HasCount(2, jobs, $"Drupal {version} connector-owned live jobs were not found.");
        return jobs;
    }

    private static void AssertDecoratedHtml(string html, JobResponse job, string baseUrl)
    {
        var document = LoadHtml(html);
        var htmlNode = document.DocumentNode.SelectSingleNode("//html");
        var body = document.DocumentNode.SelectSingleNode("//body");
        Assert.IsNotNull(htmlNode);
        Assert.IsNotNull(body);
        Assert.AreEqual(job.Source, htmlNode.GetAttributeValue("lang", string.Empty));
        Assert.AreEqual("Drupal", body.GetAttributeValue("its-rev-tool", string.Empty));
        AssertMetadata(document, "blackbird-ucid", job.ContentId);
        AssertMetadata(document, "blackbird-content-name", job.Name);
        AssertMetadata(document, "blackbird-system-ref", baseUrl);
        var atoms = document.DocumentNode.SelectNodes("//div[contains(concat(' ', normalize-space(@class), ' '), ' atom ')]");
        Assert.IsNotNull(atoms);
        var keys = atoms.Select(atom => atom.GetAttributeValue("data-blackbird-key", string.Empty)).ToArray();
        Assert.IsNotEmpty(keys);
        Assert.AreEqual(keys.Length, keys.Distinct().Count());
    }

    private static HtmlDocument LoadHtml(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        return document;
    }

    private static void AssertMetadata(HtmlDocument document, string name, string expected)
    {
        var node = document.DocumentNode.SelectSingleNode($"//meta[@name='{name}']");
        Assert.IsNotNull(node, $"Metadata '{name}' was not found.");
        Assert.AreEqual(expected, node.GetAttributeValue("content", string.Empty));
    }

    private static string ReadJobName(int version) =>
        $"Blackbird connector capture read Drupal {version}";

    private static string UploadJobName(int version) =>
        $"Blackbird connector capture upload Drupal {version}";
}
