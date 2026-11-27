using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.Models.Requests;

public class DownloadSourceContentRequest
{
    [Display("Source language", Description = "Defaults to the node's original language")]
    [DataSource(typeof(LanguagesDataHandler))]
    public string? SourceLanguage { get; set; }
}
