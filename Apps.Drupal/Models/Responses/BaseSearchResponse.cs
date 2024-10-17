using Blackbird.Applications.Sdk.Common;

namespace Apps.Drupal.Models.Responses;

public class BaseSearchResponse<T>
{
    public virtual List<T> Items { get; set; } = new ();
    
    [Display("Total count")]
    public double Total { get; set; }
}