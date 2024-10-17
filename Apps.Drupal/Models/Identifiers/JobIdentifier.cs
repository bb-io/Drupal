using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.Models.Identifiers;

public class JobIdentifier
{
    [Display("Job ID"), DataSource(typeof(JobDataHandler))]
    public string JobId { get; set; } = string.Empty;

    public override string ToString() => JobId;
}