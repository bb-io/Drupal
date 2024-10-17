using Blackbird.Applications.Sdk.Common;

namespace Apps.Drupal.Models.Responses;

public class JobSearchResponse : BaseSearchResponse<JobResponse>
{
    [Display("Jobs")]
    public override List<JobResponse> Items { get; set; } = new();
}