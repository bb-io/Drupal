using Apps.Drupal.Polling;
using Apps.Drupal.Polling.Models;
using Apps.Drupal.Polling.Models.Requests;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Polling;
using Tests.Drupal.Base;

namespace Tests.Drupal;

[TestClass]
public class PollingTests : TestBase
{
    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnTranslationJobRequested_MissingMemory_InitializesWithoutFlying(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var polling = new PollingList(context);
        var before = DateTime.UtcNow;

        // Act
        var result = await polling.OnTranslationJobRequested(
            new PollingEventRequest<DateMemory>(),
            new TranslationJobsPollingParameters());
        var after = DateTime.UtcNow;

        // Assert
        Assert.IsFalse(result.FlyBird);
        Assert.IsNull(result.Result);
        Assert.IsNotNull(result.Memory);
        Assert.IsTrue(result.Memory.LastPollingTime >= before);
        Assert.IsTrue(result.Memory.LastPollingTime <= after);
        Assert.IsEmpty(FixtureServer.Requests);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnTranslationJobRequested_MatchingTarget_ReturnsCapturedJobs(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var polling = new PollingList(context);
        var memoryTime = DateTimeOffset.FromUnixTimeSeconds(Manifest.ReadJob.Created - 1).UtcDateTime;

        // Act
        var result = await polling.OnTranslationJobRequested(
            new PollingEventRequest<DateMemory>
            {
                Memory = new DateMemory { LastPollingTime = memoryTime }
            },
            new TranslationJobsPollingParameters { TargetLanguages = [Manifest.ReadJob.Target] });

        // Assert
        Assert.IsTrue(result.FlyBird);
        Assert.IsNotNull(result.Result);
        CollectionAssert.AreEquivalent(
            new[] { Manifest.ReadJob.Id, Manifest.UploadJob.Id },
            result.Result.Items.Select(job => job.ContentId).ToArray());
        Assert.AreEqual(2, result.Result.TotalCount);
        var request = FixtureServer.LastRequest("GET", "/api/tmgmt/blackbird/jobs");
        Assert.AreEqual((Manifest.ReadJob.Created - 1).ToString(), request.Query!["created"].Single());
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnTranslationJobRequested_ExcludedTarget_ReturnsNoJobs(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var polling = new PollingList(context);

        // Act
        var result = await polling.OnTranslationJobRequested(
            new PollingEventRequest<DateMemory>
            {
                Memory = new DateMemory { LastPollingTime = DateTime.UnixEpoch }
            },
            new TranslationJobsPollingParameters { TargetLanguages = ["de"] });

        // Assert
        Assert.IsFalse(result.FlyBird);
        Assert.IsNull(result.Result);
        Assert.IsNotNull(result.Memory);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnTranslationJobRequested_ExistingMemory_AdvancesMemoryInUtc(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var polling = new PollingList(context);
        var before = DateTime.UtcNow;

        // Act
        var result = await polling.OnTranslationJobRequested(
            new PollingEventRequest<DateMemory>
            {
                Memory = new DateMemory { LastPollingTime = DateTime.UnixEpoch }
            },
            new TranslationJobsPollingParameters());
        var after = DateTime.UtcNow;

        // Assert
        Assert.IsNotNull(result.Memory);
        Assert.AreEqual(DateTimeKind.Utc, result.Memory.LastPollingTime.Kind);
        Assert.IsTrue(result.Memory.LastPollingTime >= before);
        Assert.IsTrue(result.Memory.LastPollingTime <= after);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnJobStatusChanged_MissingMemory_RecordsBaselineWithoutFlying(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var polling = new PollingList(context);

        // Act
        var result = await polling.OnJobStatusChanged(
            new PollingEventRequest<JobStatusMemory>(),
            new JobStatusChangedPollingParameters { Statuses = ["active", "rejected"] });

        // Assert
        Assert.IsFalse(result.FlyBird);
        Assert.IsNull(result.Result);
        Assert.IsNotNull(result.Memory);
        Assert.AreEqual("active", result.Memory.JobStatuses[Manifest.ReadJob.Id]);
        Assert.AreEqual("active", result.Memory.JobStatuses[Manifest.UploadJob.Id]);
        Assert.AreEqual(DateTimeKind.Utc, result.Memory.LastPollingTime.Kind);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnJobStatusChanged_MatchingStatusIdAndLabel_ReturnsChangedJob(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var polling = new PollingList(context);

        // Act
        var result = await polling.OnJobStatusChanged(
            new PollingEventRequest<JobStatusMemory>
            {
                Memory = new JobStatusMemory
                {
                    LastPollingTime = DateTime.UtcNow.AddMinutes(-5),
                    JobStatuses = new Dictionary<string, string>
                    {
                        [Manifest.ReadJob.Id] = "rejected",
                        [Manifest.UploadJob.Id] = "active"
                    }
                }
            },
            new JobStatusChangedPollingParameters
            {
                Statuses = ["active", "completed"],
                JobId = Manifest.ReadJob.Id,
                JobLabelContains = "CAPTURE READ"
            });

        // Assert
        Assert.IsTrue(result.FlyBird);
        Assert.IsNotNull(result.Result);
        Assert.HasCount(1, result.Result.Items);
        Assert.AreEqual(Manifest.ReadJob.Id, result.Result.Items[0].ContentId);
        Assert.AreEqual("rejected", result.Result.Items[0].PreviousStatus);
        Assert.AreEqual("active", result.Result.Items[0].Status);
        Assert.AreEqual("active", result.Memory!.JobStatuses[Manifest.ReadJob.Id]);
        Assert.AreEqual(1, result.Result.TotalCount);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnJobStatusChanged_UnchangedOrFilteredJobs_DoesNotFly(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var polling = new PollingList(context);

        // Act
        var result = await polling.OnJobStatusChanged(
            new PollingEventRequest<JobStatusMemory>
            {
                Memory = new JobStatusMemory
                {
                    LastPollingTime = DateTime.UtcNow.AddMinutes(-5),
                    JobStatuses = new Dictionary<string, string>
                    {
                        [Manifest.ReadJob.Id] = "active",
                        [Manifest.UploadJob.Id] = "rejected"
                    }
                }
            },
            new JobStatusChangedPollingParameters
            {
                Statuses = ["completed"],
                JobLabelContains = "upload"
            });

        // Assert
        Assert.IsFalse(result.FlyBird);
        Assert.IsNull(result.Result);
        Assert.IsNotNull(result.Memory);
        Assert.AreEqual("active", result.Memory.JobStatuses[Manifest.UploadJob.Id]);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task OnJobStatusChanged_NoStatuses_ThrowsWithoutCallingDrupal(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var polling = new PollingList(context);

        // Act
        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            polling.OnJobStatusChanged(
                new PollingEventRequest<JobStatusMemory>(),
                new JobStatusChangedPollingParameters()));

        // Assert
        StringAssert.Contains(exception.Message, "Select at least one status");
        Assert.IsEmpty(FixtureServer.Requests);
    }
}
