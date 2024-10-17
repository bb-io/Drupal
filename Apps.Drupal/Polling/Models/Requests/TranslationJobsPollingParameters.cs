using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.Polling.Models.Requests;

public class TranslationJobsPollingParameters
{
    [Display("Target languages", Description = "Specify the target languages of the jobs"), DataSource(typeof(LanguagesDataHandler))]
    public IEnumerable<string>? TargetLanguages { get; set; }
}