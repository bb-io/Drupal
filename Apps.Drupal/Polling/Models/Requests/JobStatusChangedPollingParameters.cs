using Apps.Drupal.DataSources;
using Apps.Drupal.DataSources.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.Polling.Models.Requests;

public class JobStatusChangedPollingParameters
{
    [Display("Statuses", Description = "Trigger when a job changes to any selected status"),
     StaticDataSource(typeof(JobStateDataHandler))]
    public IEnumerable<string> Statuses { get; set; } = [];

    [Display("Job ID", Description = "Trigger only for this job"), DataSource(typeof(JobDataHandler))]
    public string? JobId { get; set; }

    [Display("Job label contains", Description = "Trigger only when job label contains this text")]
    public string? JobLabelContains { get; set; }
}
