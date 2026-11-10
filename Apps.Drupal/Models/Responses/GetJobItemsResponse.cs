using Blackbird.Applications.Sdk.Common;

namespace Apps.Drupal.Models.Responses;

public class GetJobItemsResponse
{
    [Display("Items")]
    public List<JobItemResponse> Items { get; set; } = [];

    [Display("All content types match", Description = "True only if there is at least one item and every item has the same non-empty content type")]
    public bool AllContentTypesMatch { get; set; }

    [Display("First item content type", Description = "Content type of the first job item returned by Drupal, not necessarily a primary item")]
    public string? FirstItemContentType { get; set; }

    public static GetJobItemsResponse FromItems(List<JobItemResponse> items)
    {
        var firstType = items.FirstOrDefault()?.ContentType;
        return new GetJobItemsResponse
        {
            Items = items,
            FirstItemContentType = firstType,
            AllContentTypesMatch = items.Count > 0 &&
                                   !string.IsNullOrWhiteSpace(firstType) &&
                                   items.All(item => string.Equals(item.ContentType, firstType, StringComparison.Ordinal))
        };
    }
}
