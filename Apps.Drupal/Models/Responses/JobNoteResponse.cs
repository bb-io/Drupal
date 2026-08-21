using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.Drupal.Models.Responses;

public class JobNoteResponse
{
    [Display("Job ID"), JsonProperty("id")]
    public string JobId { get; set; } = string.Empty;

    [Display("Note")]
    public string Note { get; set; } = string.Empty;
}
