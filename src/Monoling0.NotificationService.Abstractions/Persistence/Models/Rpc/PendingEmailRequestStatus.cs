using System.ComponentModel;

namespace Monoling0.NotificationService.Persistence.Models.Rpc;

public enum PendingEmailRequestStatus
{
    [Description("pending")]
    Pending = 1,

    [Description("completed")]
    Completed = 2,

    [Description("failed")]
    Failed = 3,
}
