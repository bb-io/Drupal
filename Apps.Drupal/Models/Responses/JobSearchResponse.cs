using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Drupal.Models.Responses;

public class JobSearchResponse : IMultiDownloadableContentOutput<JobResponse>
{
    [Display("Jobs")]
    public List<JobResponse> Items { get; set; } = [];

    [Display("Total count")]
    public int TotalCount { get; set; }
}
