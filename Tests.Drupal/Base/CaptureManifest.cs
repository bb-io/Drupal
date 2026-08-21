namespace Tests.Drupal.Base;

public sealed class CaptureManifest
{
    public int Version { get; init; }

    public DateTime CapturedAtUtc { get; init; }

    public CapturedJob ReadJob { get; init; } = new();

    public CapturedJob UploadJob { get; init; } = new();

    public CapturedJob StatusJob { get; init; } = new();
}

public sealed class CapturedJob
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Target { get; init; } = string.Empty;

    public long Created { get; init; }
}
