using Apps.Drupal.Models.Responses;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Drupal.Polling.Models.Responses;

public class JobStatusChangedItem : JobResponse
{
    [Display("Previous status")]
    public string PreviousStatus { get; set; } = string.Empty;
}
