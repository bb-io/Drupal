using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.Models.Requests;

public class AcceptJobRequest
{
    [Display("Job ID", Description = "Unprocessed translation job to accept"), DataSource(typeof(JobDataHandler))]
    public string JobId { get; set; } = string.Empty;
}
