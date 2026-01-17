namespace Monoling0.NotificationService.Persistence.Models.Inbox;

public sealed record InboxEventPosition(string Topic, int Partition, long Offset);