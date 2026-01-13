using System.ComponentModel;

namespace Monoling0.NotificationService.Persistence.Models.Outbox;

public enum EmailOutboxStatus
{
    [Description("pending")]
    Pending = 1,

    [Description("sending")]
    Sending = 2,

    [Description("sent")]
    Sent = 3,

    [Description("failed")]
    Failed = 4,
}
