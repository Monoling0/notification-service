using Microsoft.Extensions.Logging;
using Monoling0.NotificationService.Application.Formatting;
using Monoling0.NotificationService.Persistence.Models.Rpc;
using Monoling0.NotificationService.Persistence.Repositories;
using Monoling0.NotificationService.Serialization;
using User;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeUserRpcTopic.Handlers;

public sealed class UserEmailResponseHandler
{
    private readonly IEmailOutboxRepository _emailOutboxRepository;
    private readonly IPendingEmailRequestRepository _pendingEmailRequestRepository;
    private readonly IUserEmailCacheRepository _userEmailCacheRepository;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly EmailComposer _emailComposer;
    private readonly ILogger<UserEmailResponseHandler> _logger;

    public UserEmailResponseHandler(
        IEmailOutboxRepository emailOutboxRepository,
        IPendingEmailRequestRepository pendingEmailRequestRepository,
        IUserEmailCacheRepository userEmailCacheRepository,
        IJsonSerializer jsonSerializer,
        EmailComposer emailComposer,
        ILogger<UserEmailResponseHandler> logger)
    {
        _emailOutboxRepository = emailOutboxRepository;
        _pendingEmailRequestRepository = pendingEmailRequestRepository;
        _userEmailCacheRepository = userEmailCacheRepository;
        _jsonSerializer = jsonSerializer;
        _emailComposer = emailComposer;
        _logger = logger;
    }

    public async Task HandleAsync(UserEmailResponseEvent responseEvent, CancellationToken cancellationToken)
    {
        var respondedAt = responseEvent.RespondedAt.ToDateTime();
        string email = responseEvent.Email;

        if (!string.IsNullOrWhiteSpace(email))
        {
            await _userEmailCacheRepository.UpsertAsync(
                responseEvent.UserId,
                email,
                respondedAt,
                cancellationToken);
        }

        PendingEmailRequest? pending = await _pendingEmailRequestRepository
            .FindAsync(responseEvent.CorrelationId, cancellationToken);

        if (pending is null || pending.Status != PendingEmailRequestStatus.Pending)
            return;

        if (pending.UserId != responseEvent.UserId)
        {
            _logger.LogWarning(
                "Email response user id mismatch for correlation id {CorrelationId}.",
                responseEvent.CorrelationId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(responseEvent.Error) || string.IsNullOrWhiteSpace(email))
        {
            string error = string.IsNullOrWhiteSpace(responseEvent.Error)
                ? "email_not_provided"
                : responseEvent.Error;

            await _pendingEmailRequestRepository.MarkFailedAsync(
                responseEvent.CorrelationId,
                error,
                respondedAt,
                cancellationToken);
            return;
        }

        if (pending.ExpiresAt <= DateTime.UtcNow)
        {
            await _pendingEmailRequestRepository.MarkFailedAsync(
                responseEvent.CorrelationId,
                "request_expired",
                respondedAt,
                cancellationToken);
            return;
        }

        EmailOutboxPayload payload;
        try
        {
            payload = _jsonSerializer.Deserialize<EmailOutboxPayload>(pending.Payload);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to deserialize pending email payload.");
            await _pendingEmailRequestRepository.MarkFailedAsync(
                responseEvent.CorrelationId,
                "invalid_payload",
                respondedAt,
                cancellationToken);
            return;
        }

        DateTime createdAt = DateTime.UtcNow;
        Persistence.Models.Outbox.EmailOutboxEntry entry =
            _emailComposer.CreateOutboxEntry(payload, pending.UserId, email, createdAt);
        await _emailOutboxRepository.EnqueueAsync(entry, cancellationToken);

        await _pendingEmailRequestRepository.MarkCompletedAsync(
            responseEvent.CorrelationId,
            email,
            respondedAt,
            cancellationToken);
    }
}