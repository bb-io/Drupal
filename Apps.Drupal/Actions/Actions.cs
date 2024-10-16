using Apps.Drupal.Invocables;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Drupal.Actions;

[ActionList]
public class Actions(InvocationContext invocationContext) : AppInvocable(invocationContext);