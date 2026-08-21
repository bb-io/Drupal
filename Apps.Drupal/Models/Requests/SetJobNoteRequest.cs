using Apps.Drupal.Models.Identifiers;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Drupal.Models.Requests;

public class SetJobNoteRequest : JobNoteIdentifier
{
    [Display("Note", Description = "Provider note shown on translation job. Maximum 255 characters; empty string clears note")]
    public string Note { get; set; } = string.Empty;
}
