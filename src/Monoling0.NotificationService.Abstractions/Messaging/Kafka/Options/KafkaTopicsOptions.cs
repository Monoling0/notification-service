namespace Monoling0.NotificationService.Messaging.Kafka.Options;

public sealed class KafkaTopicsOptions
{
    public string UserEvents { get; init; } = "user.events";

    public string ProgressEvents { get; init; } = "progress.events";

    public string CourseEvents { get; init; } = "course.events";

    public string FeedEvents { get; init; } = "feed.events";

    public string UserEmailRequests { get; init; } = "user.email.requests";

    public string UserEmailResponses { get; init; } = "user.email.responses";
}
