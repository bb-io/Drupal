using Apps.Drupal.DataSources;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Drupal.Polling.Models.Requests;

public class TranslationJobsPollingParameters
{
    [Display("Target languages", Description = "Trigger only for jobs with selected target languages"), DataSource(typeof(LanguagesDataHandler))]
    public IEnumerable<string>? TargetLanguages { get; set; }
}
