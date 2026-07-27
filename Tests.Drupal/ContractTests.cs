using Apps.Drupal.Actions;
using Apps.Drupal.DataSources.Static;
using Apps.Drupal.Models.Identifiers;
using Apps.Drupal.Models.Requests;
using Apps.Drupal.Models.Responses;
using Apps.Drupal.Polling;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;
using Newtonsoft.Json;

namespace Tests.Drupal;

[TestClass]
public class ContractTests
{
    [TestMethod]
    public void JobResponse_ApiArray_DeserializesBlueprintFieldsAndUnixTimestamp()
    {
        // Arrange
        const string json =
            "[{\"id\":\"42\",\"name\":\"Homepage\",\"source\":\"en\",\"target\":\"fr\",\"created\":\"1710000000\"}]";

        // Act
        var jobs = JsonConvert.DeserializeObject<List<JobResponse>>(json);

        // Assert
        Assert.IsNotNull(jobs);
        Assert.HasCount(1, jobs);
        Assert.AreEqual("42", jobs[0].ContentId);
        Assert.AreEqual("Homepage", jobs[0].Name);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeSeconds(1710000000).UtcDateTime, jobs[0].CreationDate);
        Assert.AreEqual(DateTimeKind.Utc, jobs[0].CreationDate.Kind);
    }

    [TestMethod]
    public void PublicModels_BlueprintContracts_ImplementExpectedInterfaces()
    {
        // Arrange / Act / Assert
        Assert.IsTrue(typeof(IDownloadContentInput).IsAssignableFrom(typeof(JobResponse)));
        Assert.IsTrue(typeof(IDownloadContentInput).IsAssignableFrom(typeof(JobIdentifier)));
        Assert.IsTrue(typeof(IDownloadContentOutput).IsAssignableFrom(typeof(GetXliffFromJobResponse)));
        Assert.IsTrue(typeof(IUploadContentInput).IsAssignableFrom(typeof(TranslateJobRequest)));
        Assert.IsTrue(typeof(IMultiDownloadableContentOutput<JobResponse>)
            .IsAssignableFrom(typeof(JobSearchResponse)));
    }

    [TestMethod]
    public void BlueprintMethods_DefinitionAttributes_ArePresent()
    {
        // Arrange
        var search = typeof(JobActions).GetMethod(nameof(JobActions.SearchJobsAsync));
        var download = typeof(JobActions).GetMethod(nameof(JobActions.GetXliffFromJobAsync));
        var upload = typeof(JobActions).GetMethod(nameof(JobActions.TranslateJobAsync));
        var polling = typeof(PollingList).GetMethod(nameof(PollingList.OnTranslationJobRequested));

        // Act / Assert
        Assert.HasCount(1, search!.GetCustomAttributes(typeof(BlueprintActionDefinitionAttribute), false));
        Assert.HasCount(1, download!.GetCustomAttributes(typeof(BlueprintActionDefinitionAttribute), false));
        Assert.HasCount(1, upload!.GetCustomAttributes(typeof(BlueprintActionDefinitionAttribute), false));
        Assert.HasCount(1, polling!.GetCustomAttributes(typeof(BlueprintEventDefinitionAttribute), false));
    }

    [TestMethod]
    public void JobStateDataHandler_StaticStates_ReturnsDrupalEndpointStates()
    {
        // Arrange
        var handler = new JobStateDataHandler();

        // Act
        var states = handler.GetData().ToList();

        // Assert
        CollectionAssert.AreEquivalent(
            new[] { "active", "completed", "aborted" },
            states.Select(state => state.Value).ToArray());
    }

    [TestMethod]
    public void JobActions_ProductionSurface_ContainsNoPrivateHelpers()
    {
        // Arrange / Act
        var privateMethods = typeof(JobActions)
            .GetMethods(System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToList();

        // Assert
        Assert.IsEmpty(privateMethods);
    }
}
