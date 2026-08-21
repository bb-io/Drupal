using Apps.Drupal.Api;
using Apps.Drupal.Invocables;
using Apps.Drupal.Webhooks.Models;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using RestSharp;

namespace Apps.Drupal.Webhooks.Handlers;

public class TranslationJobsSubmittedHandler(InvocationContext invocationContext)
    : AppInvocable(invocationContext), IWebhookEventHandler
{
    public async Task SubscribeAsync(
        IEnumerable<AuthenticationCredentialsProvider> creds,
        Dictionary<string, string> values)
    {
        var request = new ApiRequest("/api/tmgmt/blackbird/webhooks", Method.Post, creds)
            .AddJsonBody(new
            {
                url = values["payloadUrl"],
                events = new[] { WebhookTopics.TranslationJobsSubmitted }
            });
        await Client.ExecuteWithErrorHandling(request);
    }

    public async Task UnsubscribeAsync(
        IEnumerable<AuthenticationCredentialsProvider> creds,
        Dictionary<string, string> values)
    {
        var subscriptions = await Client.ExecuteWithErrorHandling<List<WebhookSubscription>>(
            new ApiRequest("/api/tmgmt/blackbird/webhooks", Method.Get, creds)) ?? [];
        var subscription = subscriptions.FirstOrDefault(item =>
            item.Url == values["payloadUrl"] &&
            item.Events.SequenceEqual([WebhookTopics.TranslationJobsSubmitted]));
        if (subscription is null)
        {
            return;
        }

        await Client.ExecuteWithErrorHandling(new ApiRequest(
            $"/api/tmgmt/blackbird/webhooks/{subscription.Id}", Method.Delete, creds));
    }
}
