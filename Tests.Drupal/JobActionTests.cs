using System.Text;
using Apps.Drupal.Actions;
using Apps.Drupal.Models.Identifiers;
using Apps.Drupal.Models.Requests;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using HtmlAgilityPack;
using Tests.Drupal.Base;

namespace Tests.Drupal;

[TestClass]
public class JobActionTests : TestBase
{
    [TestMethod]
    [DrupalVersionDataSource]
    public async Task SearchJobsAsync_FiltersProvided_MapsResponseAndSendsQuery(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);
        var createdAfter = DateTimeOffset.FromUnixTimeSeconds(Manifest.ReadJob.Created - 1).UtcDateTime;
        var request = new SearchJobRequest
        {
            State = "active",
            TargetLanguage = Manifest.ReadJob.Target,
            CreatedAfter = createdAfter
        };

        // Act
        var result = await actions.SearchJobsAsync(request);

        // Assert
        Assert.AreEqual(2, result.TotalCount);
        Assert.HasCount(2, result.Items);
        var readJob = result.Items.Single(job => job.ContentId == Manifest.ReadJob.Id);
        Assert.AreEqual(Manifest.ReadJob.Name, readJob.Name);
        Assert.AreEqual(Manifest.ReadJob.Source, readJob.Source);
        Assert.AreEqual(Manifest.ReadJob.Target, readJob.Target);
        Assert.AreEqual(
            DateTimeOffset.FromUnixTimeSeconds(Manifest.ReadJob.Created).UtcDateTime,
            readJob.CreationDate);

        var apiRequest = FixtureServer.LastRequest("GET", "/api/tmgmt/blackbird/jobs");
        Assert.AreEqual("active", apiRequest.Query!["state"].Single());
        Assert.AreEqual(Manifest.ReadJob.Target, apiRequest.Query["target"].Single());
        Assert.AreEqual((Manifest.ReadJob.Created - 1).ToString(), apiRequest.Query["created"].Single());
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task GetXliffFromJobAsync_OriginalFormat_PreservesCapturedDrupalHtml(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);

        // Act
        var result = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = Manifest.ReadJob.Id,
            FileFormat = "original"
        });

        // Assert
        Assert.AreEqual("text/html", result.Content.ContentType);
        Assert.AreEqual($"{Manifest.ReadJob.Id}.html", result.Content.Name);
        Assert.AreEqual(Files.ReadInputText("job.html"), Files.ReadOutputText(result.Content));
        Assert.IsFalse(Files.ReadOutputText(result.Content).Contains("blackbird-ucid", StringComparison.Ordinal));
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task GetXliffFromJobAsync_DefaultFormat_AddsBlueprintMetadataLanguagesItsAndUniqueKeys(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);

        // Act
        var result = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = Manifest.ReadJob.Id
        });

        // Assert
        var document = LoadHtml(Files.ReadOutputText(result.Content));
        var html = document.DocumentNode.SelectSingleNode("//html");
        var body = document.DocumentNode.SelectSingleNode("//body");
        Assert.IsNotNull(html);
        Assert.IsNotNull(body);
        Assert.AreEqual(Manifest.ReadJob.Source, html.GetAttributeValue("lang", string.Empty));
        Assert.AreEqual(Manifest.ReadJob.Source, html.GetAttributeValue("xml:lang", string.Empty));
        Assert.AreEqual("Drupal", body.GetAttributeValue("its-rev-tool", string.Empty));
        Assert.AreEqual(FixtureServer.Url, body.GetAttributeValue("its-rev-tool-ref", string.Empty));
        AssertMetadata(document, "blackbird-ucid", Manifest.ReadJob.Id);
        AssertMetadata(document, "blackbird-content-name", Manifest.ReadJob.Name);
        AssertMetadata(document, "blackbird-admin-url",
            $"{FixtureServer.Url}/admin/tmgmt/jobs/{Manifest.ReadJob.Id}");
        AssertMetadata(document, "blackbird-system-name", "Drupal");
        AssertMetadata(document, "blackbird-system-ref", FixtureServer.Url);
        AssertMetadata(document, "languageSource", Manifest.ReadJob.Source);
        AssertMetadata(document, "languageTarget", Manifest.ReadJob.Target);

        var atoms = document.DocumentNode.SelectNodes("//div[contains(concat(' ', normalize-space(@class), ' '), ' atom ')]");
        Assert.IsNotNull(atoms);
        Assert.IsNotEmpty(atoms);
        var keys = atoms.Select(atom => atom.GetAttributeValue("data-blackbird-key", string.Empty)).ToArray();
        Assert.AreEqual(keys.Length, keys.Distinct().Count());
        Assert.IsTrue(keys.All(key => key.StartsWith($"{Manifest.ReadJob.Id}-", StringComparison.Ordinal)));
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task TranslateJobAsync_HtmlTarget_PostsEncodedAtomsAndReturnsTargetMetadata(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);
        var download = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = Manifest.UploadJob.Id
        });
        var document = LoadHtml(Files.ReadOutputText(download.Content));
        document.DocumentNode.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' atom ')]")!
            .InnerHtml = "<strong>Translated &amp; ready</strong>";
        var translatedContent = Files.WriteOutput(
            "translated.html", document.DocumentNode.OuterHtml, "text/html");

        // Act
        var result = await actions.TranslateJobAsync(new TranslateJobRequest
        {
            Content = translatedContent,
            ContentId = Manifest.UploadJob.Id,
            Locale = Manifest.UploadJob.Target
        });

        // Assert
        var post = FixtureServer.LastRequest(
            "POST", $"/api/tmgmt/blackbird/job/{Manifest.UploadJob.Id}");
        StringAssert.Contains(post.Body!, "&lt;strong&gt;");
        StringAssert.Contains(post.Body!, "&lt;/strong&gt;");
        Assert.IsFalse(post.Body!.Contains("<strong>", StringComparison.Ordinal));
        var returned = LoadHtml(Files.ReadOutputText(result.Content));
        Assert.AreEqual(Manifest.UploadJob.Target,
            returned.DocumentNode.SelectSingleNode("//html")!.GetAttributeValue("lang", string.Empty));
        AssertMetadata(returned, "blackbird-ucid", Manifest.UploadJob.Id);
        AssertMetadata(returned, "blackbird-content-name", Manifest.UploadJob.Name);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task TranslateJobAsync_StandardUcidWithoutLegacyJobId_Uploads(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);
        var download = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = Manifest.UploadJob.Id
        });
        var document = LoadHtml(Files.ReadOutputText(download.Content));
        document.DocumentNode.SelectSingleNode("//meta[@name='JobID']")!.Remove();
        var content = Files.WriteOutput(
            "standard-ucid.html", document.DocumentNode.OuterHtml, "text/html");

        // Act
        await actions.TranslateJobAsync(new TranslateJobRequest
        {
            Content = content,
            Locale = Manifest.UploadJob.Target
        });

        // Assert
        Assert.IsNotNull(FixtureServer.LastRequest(
            "POST", $"/api/tmgmt/blackbird/job/{Manifest.UploadJob.Id}"));
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task TranslateJobAsync_LegacyRawHtmlWithoutStandardUcid_Uploads(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);
        var content = Files.WriteOutput(
            "legacy-job.html", Files.ReadInputText("job.html"), "text/html");

        // Act
        await actions.TranslateJobAsync(new TranslateJobRequest
        {
            Content = content,
            Locale = Manifest.ReadJob.Target
        });

        // Assert
        Assert.IsNotNull(FixtureServer.LastRequest(
            "POST", $"/api/tmgmt/blackbird/job/{Manifest.ReadJob.Id}"));
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task TranslateJobAsync_ConflictingEmbeddedJobIds_ThrowsMisconfigurationException(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);
        var download = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = Manifest.ReadJob.Id
        });
        var document = LoadHtml(Files.ReadOutputText(download.Content));
        document.DocumentNode.SelectSingleNode("//meta[@name='blackbird-ucid']")!
            .SetAttributeValue("content", Manifest.UploadJob.Id);
        var content = Files.WriteOutput(
            "conflicting-ids.html", document.DocumentNode.OuterHtml, "text/html");

        // Act
        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            actions.TranslateJobAsync(new TranslateJobRequest
            {
                Content = content,
                Locale = Manifest.ReadJob.Target
            }));

        // Assert
        StringAssert.Contains(exception.Message, "does not match legacy Job ID");
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task TranslateJobAsync_Xliff1Roundtrip_PreservesJobLocaleAndContent(int version)
    {
        await AssertXliffRoundtrip(version, 1);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task TranslateJobAsync_Xliff2Roundtrip_PreservesJobLocaleAndContent(int version)
    {
        await AssertXliffRoundtrip(version, 2);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task TranslateJobAsync_MissingJobId_ThrowsMisconfigurationException(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);
        var content = Files.WriteOutput("missing-job-id.html", $"""
            <html lang="{Manifest.ReadJob.Source}">
              <head>
                <meta name="languageSource" content="{Manifest.ReadJob.Source}">
                <meta name="languageTarget" content="{Manifest.ReadJob.Target}">
              </head>
              <body><div class="asset" id="asset"><div class="atom" id="title">Text</div></div></body>
            </html>
            """, "text/html");

        // Act
        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            actions.TranslateJobAsync(new TranslateJobRequest
            {
                Content = content,
                Locale = Manifest.ReadJob.Target
            }));

        // Assert
        StringAssert.Contains(exception.Message, "Job ID is missing");
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task TranslateJobAsync_ConflictingJobIds_ThrowsMisconfigurationException(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);
        var download = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = Manifest.ReadJob.Id
        });

        // Act
        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            actions.TranslateJobAsync(new TranslateJobRequest
            {
                Content = download.Content,
                ContentId = Manifest.UploadJob.Id,
                Locale = Manifest.ReadJob.Target
            }));

        // Assert
        StringAssert.Contains(exception.Message, "does not match content Job ID");
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task GetXliffFromJobAsync_UnsupportedFormat_ThrowsMisconfigurationException(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);

        // Act
        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            actions.GetXliffFromJobAsync(new JobIdentifier
            {
                ContentId = Manifest.ReadJob.Id,
                FileFormat = "application/json"
            }));

        // Assert
        StringAssert.Contains(exception.Message, "Unsupported file format");
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task TranslateJobAsync_InvalidContent_ThrowsMisconfigurationException(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);
        var content = Files.WriteOutput(
            "invalid.bin", [0x00, 0x01, 0x02, 0x03], "application/octet-stream");

        // Act
        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            actions.TranslateJobAsync(new TranslateJobRequest
            {
                Content = content,
                ContentId = Manifest.ReadJob.Id,
                Locale = Manifest.ReadJob.Target
            }));

        // Assert
        StringAssert.Contains(exception.Message, "Unsupported or invalid content file");
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task TranslateJobAsync_LocaleMismatch_ThrowsMisconfigurationException(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);
        var download = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = Manifest.ReadJob.Id
        });

        // Act
        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            actions.TranslateJobAsync(new TranslateJobRequest
            {
                Content = download.Content,
                Locale = "de"
            }));

        // Assert
        StringAssert.Contains(exception.Message, "does not match job target locale");
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task GetXliffFromJobAsync_MissingJob_ThrowsMisconfigurationException(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);

        // Act
        var exception = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(() =>
            actions.GetXliffFromJobAsync(new JobIdentifier { ContentId = "missing" }));

        // Assert
        StringAssert.Contains(exception.Message, "was not found");
    }

    private async Task AssertXliffRoundtrip(int version, int xliffVersion)
    {
        // Arrange
        var context = StartFixture(version);
        var actions = new JobActions(context, Files);
        var download = await actions.GetXliffFromJobAsync(new JobIdentifier
        {
            ContentId = Manifest.UploadJob.Id
        });
        var downloadedHtml = Files.ReadOutput(download.Content);
        var transformation = Transformation.Load(
            new MemoryStream(downloadedHtml),
            download.Content.Name,
            download.Content.ContentType).Value
            ?? throw new AssertFailedException("Downloaded HTML could not be transformed.");
        transformation.TargetLanguage = Manifest.UploadJob.Target;
        foreach (var segment in transformation.GetUnits().SelectMany(unit => unit.Segments))
        {
            segment.SetTarget(segment.GetSource());
            segment.State = SegmentState.Translated;
        }

        var sourceText = transformation.GetUnits().SelectMany(unit => unit.Segments)
            .Select(segment => segment.GetSource()).First();
        var name = $"drupal-{version}-xliff{xliffVersion}.xlf";
        var content = Files.WriteOutput(
            name,
            xliffVersion == 1
                ? Encoding.UTF8.GetBytes(Xliff1Serializer.Serialize(transformation))
                : Encoding.UTF8.GetBytes(transformation.Serialize()),
            xliffVersion == 1 ? "application/x-xliff+xml" : "application/xliff+xml");

        // Act
        var result = await actions.TranslateJobAsync(new TranslateJobRequest
        {
            Content = content,
            ContentId = Manifest.UploadJob.Id,
            Locale = Manifest.UploadJob.Target
        });

        // Assert
        Assert.AreEqual("application/xliff+xml", result.Content.ContentType);
        var returned = Transformation.Load(
            new MemoryStream(Files.ReadOutput(result.Content)),
            result.Content.Name,
            result.Content.ContentType).Value
            ?? throw new AssertFailedException("Returned XLIFF could not be transformed.");
        Assert.AreEqual(Manifest.UploadJob.Id, returned.TargetSystemReference.ContentId);
        Assert.AreEqual(Manifest.UploadJob.Target, returned.TargetLanguage);
        var returnedTargets = returned.GetUnits().SelectMany(unit => unit.Segments)
            .Select(segment => segment.GetTarget()).ToList();
        Assert.IsTrue(returnedTargets.Contains(sourceText));
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
}
