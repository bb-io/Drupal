using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.Models.Requests;

public class ReportJobErrorRequest
{
    [Display("Job ID", Description = "Active translation job to reject"), DataSource(typeof(JobDataHandler))]
    public string JobId { get; set; } = string.Empty;

    [Display("Error message", Description = "Error shown in Drupal translation job messages")]
    public string ErrorMessage { get; set; } = string.Empty;
}
