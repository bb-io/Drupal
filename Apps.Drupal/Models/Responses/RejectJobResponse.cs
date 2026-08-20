using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.Drupal.Models.Responses;

public class RejectJobResponse
{
    [Display("Job ID"), JsonProperty("id")]
    public string JobId { get; set; } = string.Empty;

    [Display("Status"), JsonProperty("state")]
    public string Status { get; set; } = string.Empty;
}
