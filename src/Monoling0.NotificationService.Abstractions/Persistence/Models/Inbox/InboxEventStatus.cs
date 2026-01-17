using System.ComponentModel;

namespace Monoling0.NotificationService.Persistence.Models.Inbox;

public enum InboxEventStatus
{
    [Description("received")]
    Received = 1,

    [Description("processed")]
    Processed = 2,

    [Description("failed")]
    Failed = 3,
}
