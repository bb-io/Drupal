using Blackbird.Filters.Shared;
using Newtonsoft.Json;

namespace Apps.Drupal.Models.Dtos;

internal class UploadSourceContentDto
{
    [JsonProperty("plugin")]
    public string SourcePlugin { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string ItemType { get; set; } = string.Empty;

    [JsonProperty("id")]
    public string ItemId { get; set; } = string.Empty;

    [JsonProperty("source")]
    public string SourceLanguage { get; set; } = string.Empty;

    [JsonProperty("target")]
    public string TargetLanguage { get; set; } = string.Empty;

    [JsonProperty("outcome")]
    public string Outcome { get; set; } = string.Empty;

    [JsonProperty("revision_id")]
    public string? RevisionId { get; set; }

    [JsonProperty("default_revision")]
    public bool? DefaultRevision { get; set; }

    [JsonProperty("skipped_fields")]
    public List<string> SkippedFields { get; set; } = [];

    [JsonProperty("warnings")]
    public List<string> Warnings { get; set; } = [];

    [JsonProperty("target_metadata")]
    public SystemReference? TargetMetadata { get; set; }
}
