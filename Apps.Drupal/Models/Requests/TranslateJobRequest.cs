using Apps.Drupal.Models.Identifiers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Drupal.Models.Requests;

public class TranslateJobRequest : JobIdentifier
{
    [Display("XLIFF file")]
    public FileReference File { get; set; } = new();
}