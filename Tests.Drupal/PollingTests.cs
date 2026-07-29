using Apps.Drupal.Polling;
using Apps.Drupal.Polling.Models;
using Apps.Drupal.Polling.Models.Requests;
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
}
