using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.Models.Identifiers;

public class JobItemsIdentifier
{
    [Display("Job ID"), DataSource(typeof(JobDataHandler))]
    public string JobId { get; set; } = string.Empty;
}
