using Apps.Drupal.Utils;
using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.Drupal.Models.Responses;

public class JobResponse
{
    [Display("Job ID")]
    public string Id { get; set; } = string.Empty;
    
    [Display("Job name")]
    public string Name { get; set; } = string.Empty;

    [Display("Source language")]
    public string Source { get; set; } = string.Empty;
    
    [Display("Target language")]
    public string Target { get; set; } = string.Empty;

    [Display("Creation date"), JsonConverter(typeof(UnixTimestampConverter)), JsonProperty("created")]
    public DateTime CreationDate { get; set; }
}