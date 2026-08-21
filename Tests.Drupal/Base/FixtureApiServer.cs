using System.Text.RegularExpressions;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Tests.Drupal.Base;

public sealed class FixtureApiServer : IDisposable
{
    public const string ValidApiKey = "fixture-api-key";
    public const string InvalidApiKey = "invalid-api-key";

    private readonly WireMockServer server;

    public FixtureApiServer(string testRoot, int version, CaptureManifest manifest)
    {
        var inputDirectory = Path.Combine(testRoot, "TestFiles", "Input", $"Drupal{version}");
        var jobs = File.ReadAllText(Path.Combine(inputDirectory, "jobs.json"));
        var languages = File.ReadAllText(Path.Combine(inputDirectory, "languages.json"));
        var readJobHtml = File.ReadAllText(Path.Combine(inputDirectory, "job.html"));
        var uploadJobHtml = ReplaceJobId(readJobHtml, manifest.ReadJob.Id, manifest.UploadJob.Id);

        server = WireMockServer.Start();
        MapGet("/api/tmgmt/blackbird/languages", languages, "application/json");
        MapGet("/api/tmgmt/blackbird/jobs", jobs, "application/json");
        foreach (var state in new[] { "unprocessed", "active", "rejected", "aborted", "completed" })
        {
            server.Given(Request.Create()
                    .WithPath("/api/tmgmt/blackbird/jobs")
                    .WithHeader("x-api-key", ValidApiKey)
                    .WithParam("state", state)
                    .UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(state == "unprocessed" ? jobs : "[]"));
        }
        MapJob(manifest.ReadJob.Id, readJobHtml);
        MapJob(manifest.UploadJob.Id, uploadJobHtml);
        MapInvalidKey("/api/tmgmt/blackbird/languages");
        MapInvalidKey("/api/tmgmt/blackbird/jobs");
    }

    public string Url => server.Urls[0];

    public IReadOnlyList<IRequestMessage> Requests =>
        server.LogEntries.Select(entry => entry.RequestMessage).OfType<IRequestMessage>().ToList();

    public IRequestMessage LastRequest(string method, string path) => Requests.Last(request =>
        string.Equals(request.Method, method, StringComparison.OrdinalIgnoreCase) && request.Path == path);

    public void Dispose()
    {
        server.Stop();
        server.Dispose();
    }

    private void MapGet(string path, string body, string contentType)
    {
        server.Given(Request.Create()
                .WithPath(path)
                .WithHeader("x-api-key", ValidApiKey)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", contentType)
                .WithBody(body));
    }

    private void MapJob(string jobId, string html)
    {
        var path = $"/api/tmgmt/blackbird/job/{jobId}";
        var acceptPath = $"{path}/accept";
        var rejectPath = $"{path}/reject";
        MapGet(path, html, "text/html; charset=UTF-8");
        server.Given(Request.Create()
                .WithPath(path)
                .WithHeader("x-api-key", ValidApiKey)
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));
        server.Given(Request.Create()
                .WithPath(acceptPath)
                .WithHeader("x-api-key", ValidApiKey)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{\"id\":\"{jobId}\",\"state\":\"active\"}}"));
        server.Given(Request.Create()
                .WithPath(rejectPath)
                .WithHeader("x-api-key", ValidApiKey)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{\"id\":\"{jobId}\",\"state\":\"rejected\"}}"));
        MapInvalidKey(path);
        MapInvalidKey(acceptPath);
        MapInvalidKey(rejectPath);
    }

    private void MapInvalidKey(string path)
    {
        server.Given(Request.Create()
                .WithPath(path)
                .WithHeader("x-api-key", InvalidApiKey))
            .RespondWith(Response.Create()
                .WithStatusCode(403)
                .WithHeader("Content-Type", "text/html")
                .WithBody("<html><head><title>Access denied</title></head><body>Missing or invalid API key.</body></html>"));
    }

    private static string ReplaceJobId(string html, string sourceId, string targetId) => Regex.Replace(
        html,
        $"(<meta\\s+name=[\\\"']JobID[\\\"']\\s+content=[\\\"']){Regex.Escape(sourceId)}([\\\"'])",
        $"${{1}}{targetId}${{2}}",
        RegexOptions.IgnoreCase);
}
