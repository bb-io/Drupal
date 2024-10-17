using Apps.Drupal.DataSources;
using Apps.Drupal.DataSources.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.Models.Requests;

public class SearchJobRequest
{
    [Display("State", Description = "Specify the state of the job. By default it will return only active jobs"), StaticDataSource(typeof(JobStateDataHandler))]
    public string? State { get; set; }
    
    [Display("Target language", Description = "Specify the target language of the job"), DataSource(typeof(LanguagesDataHandler))]
    public string? TargetLanguage { get; set; }
    
    [Display("Created after", Description = "Specify the date after which the job was created")]
    public DateTime? CreatedAfter { get; set; }
}