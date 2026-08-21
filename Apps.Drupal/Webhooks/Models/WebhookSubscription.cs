using Newtonsoft.Json;

namespace Apps.Drupal.Webhooks.Models;

public class WebhookSubscription
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("events")]
    public List<string> Events { get; set; } = [];
}
