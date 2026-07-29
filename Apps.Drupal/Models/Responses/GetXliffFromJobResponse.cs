using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Drupal.Models.Responses;

public class GetXliffFromJobResponse : IDownloadContentOutput
{
    [Display("Content")]
    public FileReference Content { get; set; } = default!;
}
