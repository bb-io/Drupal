using Apps.Drupal.DataSources;
using Apps.Drupal.DataSources.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.Models.Requests;

public class SearchJobRequest
{
    [Display("State", Description = "Filter jobs by state. Defaults to unprocessed jobs"), StaticDataSource(typeof(JobStateDataHandler))]
    public string? State { get; set; }
    
    [Display("Target language", Description = "Filter jobs by target language"), DataSource(typeof(LanguagesDataHandler))]
    public string? TargetLanguage { get; set; }
    
    [Display("Created after", Description = "Filter jobs created on or after this date")]
    public DateTime? CreatedAfter { get; set; }
}
