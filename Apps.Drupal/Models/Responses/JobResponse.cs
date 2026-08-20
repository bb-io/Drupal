using Apps.Drupal.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;
using Newtonsoft.Json;

namespace Apps.Drupal.Models.Responses;

public class JobResponse : IDownloadContentInput
{
    [Display("Job ID"), JsonProperty("id")]
    public string ContentId { get; set; } = string.Empty;

    [Display("Job name")]
    public string Name { get; set; } = string.Empty;

    [Display("Source language")]
    public string Source { get; set; } = string.Empty;

    [Display("Target language")]
    public string Target { get; set; } = string.Empty;

    [Display("Status")]
    public string Status { get; set; } = string.Empty;

    [Display("Creation date"), JsonConverter(typeof(UnixTimestampConverter)), JsonProperty("created")]
    public DateTime CreationDate { get; set; }
}
