using Microsoft.Extensions.Logging;
using Monoling0.NotificationService.Application.UseCases.ConsumeProgressTopic.Handlers;
using Monoling0.NotificationService.Messaging.Kafka.Models;
using Monoling0.NotificationService.UseCases;
using Progress;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeProgressTopic;

public sealed class ProgressTopicUseCase : IEventHandler<ProgressEventEnvelope>
{
    private readonly LessonCompletedHandler _lessonCompletedHandler;
    private readonly CourseCompletedHandler _courseCompletedHandler;
    private readonly ILogger<ProgressTopicUseCase> _logger;

    public ProgressTopicUseCase(
        LessonCompletedHandler lessonCompletedHandler,
        CourseCompletedHandler courseCompletedHandler,
        ILogger<ProgressTopicUseCase> logger)
    {
        _lessonCompletedHandler = lessonCompletedHandler;
        _courseCompletedHandler = courseCompletedHandler;
        _logger = logger;
    }

    public async Task HandleAsync(
        KafkaConsumedMessage<ProgressEventEnvelope> message,
        CancellationToken cancellationToken)
    {
        ProgressEventEnvelope envelope = message.Event;

        switch (envelope.EventCase)
        {
            case ProgressEventEnvelope.EventOneofCase.LessonCompleted:
                await _lessonCompletedHandler.HandleAsync(envelope.LessonCompleted, cancellationToken);
                break;
            case ProgressEventEnvelope.EventOneofCase.CourseCompleted:
                await _courseCompletedHandler.HandleAsync(envelope.CourseCompleted, cancellationToken);
                break;
            case ProgressEventEnvelope.EventOneofCase.CourseEnrolled:
                break;
            case ProgressEventEnvelope.EventOneofCase.None:
                _logger.LogWarning("Progress event envelope with empty payload received.");
                break;
            default:
                _logger.LogWarning("Unhandled progress event type {EventType}.", envelope.EventCase);
                break;
        }
    }
}
