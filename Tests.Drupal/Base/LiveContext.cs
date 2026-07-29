namespace Tests.Drupal.Base;

public sealed class LiveContext
{
    public int Version { get; init; }

    public string BaseUrl { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public override string ToString() => $"Drupal {Version}";
}
