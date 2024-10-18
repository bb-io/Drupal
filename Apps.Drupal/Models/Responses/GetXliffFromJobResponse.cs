using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Drupal.Models.Responses;

public class GetXliffFromJobResponse
{
    public FileReference File { get; set; } = default!;
}