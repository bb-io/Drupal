using Apps.Drupal.Utils;
using Newtonsoft.Json;

namespace Apps.Drupal.Models.Dtos;

internal class SourceNodeDto
{
    [JsonProperty("id")]
    public string NodeId { get; set; } = string.Empty;

    [JsonProperty("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [JsonProperty("source_language")]
    public string SourceLanguage { get; set; } = string.Empty;

    [JsonProperty("available_languages")]
    public List<string> AvailableLanguages { get; set; } = [];

    [JsonProperty("published")]
    public bool Published { get; set; }

    [JsonProperty("author_id")]
    public string? AuthorId { get; set; }

    [JsonConverter(typeof(UnixTimestampConverter)), JsonProperty("created")]
    public DateTime CreationDate { get; set; }

    [JsonConverter(typeof(UnixTimestampConverter)), JsonProperty("changed")]
    public DateTime LastModifiedDate { get; set; }

    [JsonProperty("revision_id")]
    public string RevisionId { get; set; } = string.Empty;

    [JsonProperty("source_url")]
    public string SourceUrl { get; set; } = string.Empty;

    [JsonProperty("admin_url")]
    public string? AdminUrl { get; set; }
}
