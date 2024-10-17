using Apps.Drupal.DataSources.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Drupal.Models.Requests;

public class SearchJobRequest
{
    [Display("State", Description = "Specify the state of the job"), StaticDataSource(typeof(JobStateDataHandler))]
    public string? State { get; set; }
    
    [Display("Created after", Description = "Specify the date after which the job was created")]
    public DateTime? CreatedAfter { get; set; }
}