using Newtonsoft.Json;

namespace Apps.Drupal.Webhooks.Models;

public class DrupalWebhookPayload
{
    [JsonProperty("event_id")]
    public string EventId { get; set; } = string.Empty;

    [JsonProperty("event")]
    public string Event { get; set; } = string.Empty;

    [JsonProperty("occurred_at")]
    public DateTime OccurredAt { get; set; }

    [JsonProperty("jobs")]
    public List<DrupalWebhookJob> Jobs { get; set; } = [];
}

public class DrupalWebhookJob
{
    [JsonProperty("job_id")]
    public string JobId { get; set; } = string.Empty;

    [JsonProperty("source_language")]
    public string SourceLanguage { get; set; } = string.Empty;

    [JsonProperty("target_language")]
    public string TargetLanguage { get; set; } = string.Empty;

    [JsonProperty("previous_status")]
    public string PreviousStatus { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("items")]
    public List<DrupalWebhookJobItem> Items { get; set; } = [];
}

public class DrupalWebhookJobItem
{
    [JsonProperty("job_item_id")]
    public string JobItemId { get; set; } = string.Empty;

    [JsonProperty("source_plugin")]
    public string SourcePlugin { get; set; } = string.Empty;

    [JsonProperty("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [JsonProperty("content_id")]
    public string ContentId { get; set; } = string.Empty;
}
