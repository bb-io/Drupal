using Blackbird.Applications.Sdk.Common;

namespace Apps.Drupal.Models.Identifiers;

public class SourceNodeIdentifier
{
    [Display("Node ID")]
    public string NodeId { get; set; } = string.Empty;
}
