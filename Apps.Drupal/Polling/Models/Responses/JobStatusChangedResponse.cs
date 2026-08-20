using Blackbird.Applications.Sdk.Common;

namespace Apps.Drupal.Polling.Models.Responses;

public class JobStatusChangedResponse
{
    [Display("Jobs")]
    public List<JobStatusChangedItem> Items { get; set; } = [];

    [Display("Total count")]
    public int TotalCount { get; set; }
}
