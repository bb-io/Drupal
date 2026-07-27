using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Tests.Drupal.Base;

namespace Tests.Drupal;

[TestClass]
public class DataHandlerTests : TestBase
{
    [TestMethod]
    [DrupalVersionDataSource]
    public async Task LanguagesDataHandler_CapturedLanguages_ReturnsValuesAndSearchFiltering(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var handler = new LanguagesDataHandler(context);

        // Act
        var all = (await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None)).ToList();
        var filtered = (await handler.GetDataAsync(
            new DataSourceContext { SearchString = "fRe" }, CancellationToken.None)).ToList();

        // Assert
        CollectionAssert.AreEquivalent(new[] { "en", "fr" }, all.Select(item => item.Value).ToArray());
        Assert.HasCount(1, filtered);
        Assert.AreEqual("fr", filtered[0].Value);
        Assert.AreEqual("French", filtered[0].DisplayName);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task JobDataHandler_CapturedJobs_ReturnsValuesAndSearchFiltering(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var handler = new JobDataHandler(context);

        // Act
        var all = (await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None)).ToList();
        var filtered = (await handler.GetDataAsync(
            new DataSourceContext { SearchString = "UPLOAD" }, CancellationToken.None)).ToList();

        // Assert
        CollectionAssert.AreEquivalent(
            new[] { Manifest.ReadJob.Id, Manifest.UploadJob.Id },
            all.Select(item => item.Value).ToArray());
        Assert.HasCount(1, filtered);
        Assert.AreEqual(Manifest.UploadJob.Id, filtered[0].Value);
        Assert.AreEqual(Manifest.UploadJob.Name, filtered[0].DisplayName);
    }
}
