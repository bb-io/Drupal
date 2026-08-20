using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.Models.Requests;

public class RejectJobRequest
{
    [Display("Job ID", Description = "Active translation job to reject"), DataSource(typeof(JobDataHandler))]
    public string JobId { get; set; } = string.Empty;

    [Display("Rejection reason", Description = "Reason shown in Drupal translation job messages")]
    public string RejectionReason { get; set; } = string.Empty;
}
