using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.Drupal.Models.Responses;

public class JobItemResponse
{
    [Display("Job item ID"), JsonProperty("job_item_id")]
    public string JobItemId { get; set; } = string.Empty;

    [Display("Source plugin"), JsonProperty("source_plugin")]
    public string SourcePlugin { get; set; } = string.Empty;

    [Display("Item type"), JsonProperty("item_type")]
    public string ItemType { get; set; } = string.Empty;

    [Display("Item ID"), JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    [Display("Label"), JsonProperty("label")]
    public string Label { get; set; } = string.Empty;

    [Display("Content type", Description = "Drupal bundle, such as article or page, when available"), JsonProperty("content_type")]
    public string? ContentType { get; set; }

    [Display("Source URL"), JsonProperty("source_url")]
    public string? SourceUrl { get; set; }

    [Display("Admin URL"), JsonProperty("admin_url")]
    public string? AdminUrl { get; set; }
}
