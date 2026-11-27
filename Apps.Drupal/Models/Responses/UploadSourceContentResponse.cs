using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Drupal.Models.Responses;

public class UploadSourceContentResponse
{
    [Display("Source plugin")]
    public string SourcePlugin { get; set; } = string.Empty;

    [Display("Item type")]
    public string ItemType { get; set; } = string.Empty;

    [Display("Item ID")]
    public string ItemId { get; set; } = string.Empty;

    [Display("Source language")]
    public string SourceLanguage { get; set; } = string.Empty;

    [Display("Target language")]
    public string TargetLanguage { get; set; } = string.Empty;

    public string Outcome { get; set; } = string.Empty;

    [Display("Revision ID")]
    public string? RevisionId { get; set; }

    [Display("Default revision")]
    public bool? DefaultRevision { get; set; }

    [Display("Target content")]
    public FileReference? Content { get; set; }

    [Display("Skipped field keys")]
    public List<string> SkippedFields { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}
