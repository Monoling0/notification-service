using Course;
using Microsoft.Extensions.Logging;
using Monoling0.NotificationService.Application.UseCases.ConsumeCourseTopic.Handlers;
using Monoling0.NotificationService.Messaging.Kafka.Models;
using Monoling0.NotificationService.UseCases;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeCourseTopic;

public sealed class CourseTopicUseCase : IEventHandler<CourseEventEnvelope>
{
    private readonly CoursePublishedHandler _coursePublishedHandler;
    private readonly CourseUpdatedHandler _courseUpdatedHandler;
    private readonly CourseUnpublishedHandler _courseUnpublishedHandler;
    private readonly ILogger<CourseTopicUseCase> _logger;

    public CourseTopicUseCase(
        CoursePublishedHandler coursePublishedHandler,
        CourseUpdatedHandler courseUpdatedHandler,
        CourseUnpublishedHandler courseUnpublishedHandler,
        ILogger<CourseTopicUseCase> logger)
    {
        _coursePublishedHandler = coursePublishedHandler;
        _courseUpdatedHandler = courseUpdatedHandler;
        _courseUnpublishedHandler = courseUnpublishedHandler;
        _logger = logger;
    }

    public async Task HandleAsync(
        KafkaConsumedMessage<CourseEventEnvelope> message,
        CancellationToken cancellationToken)
    {
        CourseEventEnvelope envelope = message.Event;

        switch (envelope.EventCase)
        {
            case CourseEventEnvelope.EventOneofCase.CoursePublished:
                await _coursePublishedHandler.HandleAsync(envelope.CoursePublished, cancellationToken);
                break;
            case CourseEventEnvelope.EventOneofCase.CourseUpdated:
                await _courseUpdatedHandler.HandleAsync(envelope.CourseUpdated, cancellationToken);
                break;
            case CourseEventEnvelope.EventOneofCase.CourseUnpublished:
                await _courseUnpublishedHandler.HandleAsync(envelope.CourseUnpublished, cancellationToken);
                break;
            case CourseEventEnvelope.EventOneofCase.None:
                _logger.LogWarning("Course event envelope with empty payload received.");
                break;
            default:
                _logger.LogWarning("Unhandled course event type {EventType}.", envelope.EventCase);
                break;
        }
    }
}
