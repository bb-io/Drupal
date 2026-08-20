namespace Apps.Drupal.Polling.Models;

public class JobStatusMemory
{
    public DateTime LastPollingTime { get; set; }

    public Dictionary<string, string> JobStatuses { get; set; } = [];
}
