using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monoling0.NotificationService.Application.Formatting;
using Monoling0.NotificationService.Application.Messaging;
using Monoling0.NotificationService.Messaging.Kafka;
using Monoling0.NotificationService.Messaging.Kafka.Models;
using Monoling0.NotificationService.Messaging.Kafka.Options;
using Monoling0.NotificationService.Persistence.Models.Outbox;
using Monoling0.NotificationService.Persistence.Models.Rpc;
using Monoling0.NotificationService.Persistence.Repositories;
using Monoling0.NotificationService.Serialization;
using User;

namespace Monoling0.NotificationService.Application.UseCases.Common;

public sealed class EmailNotificationService
{
    private static readonly TimeSpan PendingRequestTimeToLive = TimeSpan.FromDays(1);

    private readonly EmailComposer _emailComposer;
    private readonly IEmailOutboxRepository _emailOutboxRepository;
    private readonly IUserEmailCacheRepository _userEmailCacheRepository;
    private readonly IPendingEmailRequestRepository _pendingEmailRequestRepository;
    private readonly IFollowersCacheRepository _followersCacheRepository;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly KafkaTopicName _userEmailRequestsTopic;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        EmailComposer emailComposer,
        IEmailOutboxRepository emailOutboxRepository,
        IUserEmailCacheRepository userEmailCacheRepository,
        IPendingEmailRequestRepository pendingEmailRequestRepository,
        IFollowersCacheRepository followersCacheRepository,
        IKafkaProducer kafkaProducer,
        IJsonSerializer jsonSerializer,
        IOptions<KafkaTopicsOptions> kafkaTopicsOptions,
        ILogger<EmailNotificationService> logger)
    {
        _emailComposer = emailComposer;
        _emailOutboxRepository = emailOutboxRepository;
        _userEmailCacheRepository = userEmailCacheRepository;
        _pendingEmailRequestRepository = pendingEmailRequestRepository;
        _followersCacheRepository = followersCacheRepository;
        _kafkaProducer = kafkaProducer;
        _jsonSerializer = jsonSerializer;
        _userEmailRequestsTopic = new KafkaTopicName(kafkaTopicsOptions.Value.UserEmailRequests);
        _logger = logger;
    }

    public async Task SendToUserAsync(
        string kind,
        object model,
        long userId,
        CancellationToken cancellationToken)
    {
        EmailOutboxPayload payload = _emailComposer.Compose(kind, model);
        await SendPayloadToUserAsync(payload, userId, cancellationToken);
    }

    public async Task SendToUsersAsync(
        string kind,
        object model,
        IReadOnlyCollection<long> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return;

        EmailOutboxPayload payload = _emailComposer.Compose(kind, model);
        await SendPayloadToUsersAsync(payload, userIds, cancellationToken);
    }

    public async Task SendToAllUsersAsync(
        string kind,
        object model,
        CancellationToken cancellationToken)
    {
        EmailOutboxPayload payload = _emailComposer.Compose(kind, model);
        await SendPayloadToAllUsersAsync(payload, cancellationToken);
    }

    public async Task SendToFollowersAsync(
        long followeeId,
        string kind,
        object model,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<long> followerIds =
            await _followersCacheRepository.GetFollowersAsync(followeeId, cancellationToken);

        if (followerIds.Count == 0)
            return;

        await SendToUsersAsync(kind, model, followerIds, cancellationToken);
    }

    private async Task SendPayloadToUserAsync(
        EmailOutboxPayload payload,
        long userId,
        CancellationToken cancellationToken)
    {
        string? email = await _userEmailCacheRepository.TryGetEmailAsync(userId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(email))
        {
            await EnqueueEmailAsync(payload, userId, email, cancellationToken);
            return;
        }

        await CreatePendingRequestAsync(userId, payload, cancellationToken);
    }

    private async Task SendPayloadToUsersAsync(
        EmailOutboxPayload payload,
        IReadOnlyCollection<long> userIds,
        CancellationToken cancellationToken)
    {
        var uniqueUserIds = new HashSet<long>(userIds);
        Dictionary<long, string> emails = await _userEmailCacheRepository
            .GetEmailsAsync(uniqueUserIds, cancellationToken);

        foreach (KeyValuePair<long, string> entry in emails)
            await EnqueueEmailAsync(payload, entry.Key, entry.Value, cancellationToken);

        foreach (long userId in uniqueUserIds)
        {
            if (emails.ContainsKey(userId))
                continue;

            await CreatePendingRequestAsync(userId, payload, cancellationToken);
        }
    }

    private async Task SendPayloadToAllUsersAsync(
        EmailOutboxPayload payload,
        CancellationToken cancellationToken)
    {
        Dictionary<long, string> emails = await _userEmailCacheRepository.GetAllEmailsAsync(cancellationToken);

        foreach (KeyValuePair<long, string> entry in emails)
            await EnqueueEmailAsync(payload, entry.Key, entry.Value, cancellationToken);
    }

    private async Task EnqueueEmailAsync(
        EmailOutboxPayload payload,
        long userId,
        string email,
        CancellationToken cancellationToken)
    {
        DateTime createdAtUtc = DateTime.UtcNow;
        EmailOutboxEntry outboxEntry = _emailComposer.CreateOutboxEntry(payload, userId, email, createdAtUtc);
        await _emailOutboxRepository.EnqueueAsync(outboxEntry, cancellationToken);
    }

    private async Task CreatePendingRequestAsync(
        long userId,
        EmailOutboxPayload payload,
        CancellationToken cancellationToken)
    {
        string correlationId = Guid.NewGuid().ToString("N");
        DateTime createdAtUtc = DateTime.UtcNow;
        string payloadJson = _jsonSerializer.Serialize(payload);

        var request = new PendingEmailRequest
        {
            CorrelationId = correlationId,
            UserId = userId,
            Purpose = payload.Kind,
            Payload = payloadJson,
            Status = PendingEmailRequestStatus.Pending,
            CreatedAt = createdAtUtc,
            ExpiresAt = createdAtUtc.Add(PendingRequestTimeToLive),
        };

        await _pendingEmailRequestRepository.CreateAsync(request, cancellationToken);

        var requestEvent = new UserEmailRequestEvent
        {
            CorrelationId = correlationId,
            UserId = userId,
            Purpose = payload.Kind,
            PayloadJson = payloadJson,
            RequestedAt = Timestamp.FromDateTime(createdAtUtc),
        };

        var envelope = new UserRpcEventEnvelope
        {
            Meta = EventMetaFactory.Create(correlationId, correlationId),
            UserEmailRequest = requestEvent,
        };

        await _kafkaProducer.ProduceAsync(
            _userEmailRequestsTopic,
            new KafkaMessageKey(correlationId),
            envelope,
            null,
            cancellationToken);

        _logger.LogInformation(
            "Requested user email for {UserId} with correlation id {CorrelationId}.",
            userId,
            correlationId);
    }
}
