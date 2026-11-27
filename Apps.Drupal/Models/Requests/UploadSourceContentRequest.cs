using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Drupal.Models.Requests;

public class UploadSourceContentRequest
{
    [Display("Content", Description = "Translated file from Download source content")]
    public FileReference Content { get; set; } = new();

    [Display("Target language", Description = "Enabled language to save in Drupal")]
    [DataSource(typeof(LanguagesDataHandler))]
    public string TargetLanguage { get; set; } = string.Empty;

    [Display("Publish", Description = "Publish the node translation immediately. Defaults to saving a draft.")]
    public bool? Publish { get; set; }
}
