using Blackbird.Applications.Sdk.Common;

namespace Apps.Drupal.Models.Responses;

public class SourceNodeResponse
{
    [Display("Node ID")]
    public string NodeId { get; set; } = string.Empty;

    [Display("Node UUID")]
    public string Uuid { get; set; } = string.Empty;

    public string? Title { get; set; }

    [Display("Content type")]
    public string ContentType { get; set; } = string.Empty;

    [Display("Source language")]
    public string SourceLanguage { get; set; } = string.Empty;

    [Display("Available languages")]
    public List<string> AvailableLanguages { get; set; } = [];

    public bool Published { get; set; }

    [Display("Author ID")]
    public string? AuthorId { get; set; }

    [Display("Creation date")]
    public DateTime CreationDate { get; set; }

    [Display("Last modified date")]
    public DateTime LastModifiedDate { get; set; }

    [Display("Revision ID")]
    public string RevisionId { get; set; } = string.Empty;

    [Display("Source URL")]
    public string SourceUrl { get; set; } = string.Empty;

    [Display("Admin URL")]
    public string? AdminUrl { get; set; }
}
