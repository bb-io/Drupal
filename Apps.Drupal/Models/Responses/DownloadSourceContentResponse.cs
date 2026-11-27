using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Drupal.Models.Responses;

public class DownloadSourceContentResponse
{
    public FileReference Content { get; set; } = new();
}
