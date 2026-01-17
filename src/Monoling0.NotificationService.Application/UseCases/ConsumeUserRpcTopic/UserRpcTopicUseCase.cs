using Microsoft.Extensions.Logging;
using Monoling0.NotificationService.Application.UseCases.ConsumeUserRpcTopic.Handlers;
using Monoling0.NotificationService.Messaging.Kafka.Models;
using Monoling0.NotificationService.UseCases;
using User;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeUserRpcTopic;

public sealed class UserRpcTopicUseCase : IEventHandler<UserRpcEventEnvelope>
{
    private readonly UserEmailResponseHandler _userEmailResponseHandler;
    private readonly UserEmailsBatchResponseHandler _userEmailsBatchResponseHandler;
    private readonly ILogger<UserRpcTopicUseCase> _logger;

    public UserRpcTopicUseCase(
        UserEmailResponseHandler userEmailResponseHandler,
        UserEmailsBatchResponseHandler userEmailsBatchResponseHandler,
        ILogger<UserRpcTopicUseCase> logger)
    {
        _userEmailResponseHandler = userEmailResponseHandler;
        _userEmailsBatchResponseHandler = userEmailsBatchResponseHandler;
        _logger = logger;
    }

    public async Task HandleAsync(
        KafkaConsumedMessage<UserRpcEventEnvelope> message,
        CancellationToken cancellationToken)
    {
        UserRpcEventEnvelope envelope = message.Event;

        switch (envelope.EventCase)
        {
            case UserRpcEventEnvelope.EventOneofCase.UserEmailResponse:
                await _userEmailResponseHandler.HandleAsync(envelope.UserEmailResponse, cancellationToken);
                break;
            case UserRpcEventEnvelope.EventOneofCase.UserEmailsResponse:
                await _userEmailsBatchResponseHandler.HandleAsync(envelope.UserEmailsResponse, cancellationToken);
                break;
            case UserRpcEventEnvelope.EventOneofCase.None:
                _logger.LogWarning("User RPC event envelope with empty payload received.");
                break;
            default:
                _logger.LogWarning("Unhandled user RPC event type {EventType}.", envelope.EventCase);
                break;
        }
    }
}
