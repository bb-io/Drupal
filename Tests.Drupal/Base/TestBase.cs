using Apps.Drupal.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Tests.Drupal.Base;

public abstract class TestBase
{
    public static readonly int[] SupportedVersions = [8, 9, 10, 11];

    private FixtureApiServer? fixtureServer;

    public TestContext TestContext { get; set; } = null!;

    protected static string TestRoot { get; } = FindTestRoot();

    protected FileManager Files { get; private set; } = null!;

    protected CaptureManifest Manifest { get; private set; } = null!;

    protected FixtureApiServer FixtureServer => fixtureServer
        ?? throw new InvalidOperationException("Fixture server was not started.");

    protected InvocationContext StartFixture(int version)
    {
        fixtureServer?.Dispose();
        Manifest = LoadManifest(version);
        Files = CreateFileManager(version);
        fixtureServer = new FixtureApiServer(TestRoot, version, Manifest);
        return CreateInvocationContext(fixtureServer.Url, FixtureApiServer.ValidApiKey);
    }

    protected InvocationContext CreateLiveInvocationContext(LiveContext context)
    {
        Manifest = LoadManifest(context.Version);
        Files = CreateFileManager(context.Version);
        return CreateInvocationContext(context.BaseUrl, context.ApiKey);
    }

    protected static InvocationContext CreateInvocationContext(string baseUrl, string apiKey) => new()
    {
        AuthenticationCredentialsProviders =
        [
            new AuthenticationCredentialsProvider(CredsNames.BaseUrl, baseUrl.TrimEnd('/')),
            new AuthenticationCredentialsProvider(CredsNames.ApiKey, apiKey)
        ]
    };

    protected static CaptureManifest LoadManifest(int version)
    {
        var path = Path.Combine(TestRoot, "TestFiles", "Input", $"Drupal{version}", "manifest.json");
        return JsonConvert.DeserializeObject<CaptureManifest>(File.ReadAllText(path))
            ?? throw new InvalidDataException($"Capture manifest is invalid: {path}");
    }

    public static IReadOnlyList<LiveContext> LoadLiveContexts()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(TestRoot)
            .AddJsonFile("appsettings.json.example", optional: false)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var configuredContexts = configuration.GetSection("LiveContexts").GetChildren().ToList();
        if (configuredContexts.Count != SupportedVersions.Length)
        {
            throw new InvalidDataException("LiveContexts must contain four entries ordered for Drupal 8, 9, 10, and 11.");
        }

        var contexts = configuredContexts
            .Select((section, index) => new LiveContext
            {
                Version = SupportedVersions[index],
                BaseUrl = section[nameof(LiveContext.BaseUrl)]
                    ?? throw new InvalidDataException("Live context BaseUrl is required."),
                ApiKey = section[nameof(LiveContext.ApiKey)]
                    ?? throw new InvalidDataException("Live context ApiKey is required.")
            })
            .ToList();

        return contexts;
    }

    [TestCleanup]
    public void DisposeFixtureServer()
    {
        fixtureServer?.Dispose();
        fixtureServer = null;
    }

    private FileManager CreateFileManager(int version) =>
        new(TestRoot, version, $"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}");

    private static string FindTestRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Tests.Drupal.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Tests.Drupal project root was not found.");
    }
}
