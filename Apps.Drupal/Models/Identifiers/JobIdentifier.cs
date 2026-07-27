using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Blueprints.Handlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Drupal.Models.Identifiers;

public class JobIdentifier : IDownloadContentInput
{
    [Display("Job ID"), DataSource(typeof(JobDataHandler))]
    public string ContentId { get; set; } = string.Empty;

    [Display("File format", Description = "Select original to download content without interoperability metadata")]
    [StaticDataSource(typeof(DownloadFileFormatHandler))]
    public string? FileFormat { get; set; }

    public override string ToString() => ContentId;
}
