using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Drupal.Models.Requests;

public class TranslateJobRequest : IUploadContentInput
{
    [Display("Content", Description = "Translated job content to upload")]
    public FileReference Content { get; set; } = new();

    [Display("Target language", Description = "Must match target language configured for translation job")]
    public string Locale { get; set; } = string.Empty;

    [Display("Job ID", Description = "Override job ID embedded in content"), DataSource(typeof(JobDataHandler))]
    public string? ContentId { get; set; }
}
