using Feed;
using Microsoft.Extensions.Logging;
using Monoling0.NotificationService.Application.UseCases.ConsumeFeedTopic.Handlers;
using Monoling0.NotificationService.Messaging.Kafka.Models;
using Monoling0.NotificationService.UseCases;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeFeedTopic;

public sealed class FeedTopicUseCase : IEventHandler<FeedEventEnvelope>
{
    private readonly StreakMilestoneHandler _streakMilestoneHandler;
    private readonly StreakBrokenHandler _streakBrokenHandler;
    private readonly LeaderboardPrizeHandler _leaderboardPrizeHandler;
    private readonly WeeklyReportHandler _weeklyReportHandler;
    private readonly ILogger<FeedTopicUseCase> _logger;

    public FeedTopicUseCase(
        StreakMilestoneHandler streakMilestoneHandler,
        StreakBrokenHandler streakBrokenHandler,
        LeaderboardPrizeHandler leaderboardPrizeHandler,
        WeeklyReportHandler weeklyReportHandler,
        ILogger<FeedTopicUseCase> logger)
    {
        _streakMilestoneHandler = streakMilestoneHandler;
        _streakBrokenHandler = streakBrokenHandler;
        _leaderboardPrizeHandler = leaderboardPrizeHandler;
        _weeklyReportHandler = weeklyReportHandler;
        _logger = logger;
    }

    public async Task HandleAsync(KafkaConsumedMessage<FeedEventEnvelope> message, CancellationToken cancellationToken)
    {
        FeedEventEnvelope envelope = message.Event;

        switch (envelope.EventCase)
        {
            case FeedEventEnvelope.EventOneofCase.StreakMilestone:
                await _streakMilestoneHandler.HandleAsync(envelope.StreakMilestone, cancellationToken);
                break;
            case FeedEventEnvelope.EventOneofCase.StreakBroken:
                await _streakBrokenHandler.HandleAsync(envelope.StreakBroken, cancellationToken);
                break;
            case FeedEventEnvelope.EventOneofCase.LeaderboardPrize:
                await _leaderboardPrizeHandler.HandleAsync(envelope.LeaderboardPrize, cancellationToken);
                break;
            case FeedEventEnvelope.EventOneofCase.WeeklyReport:
                await _weeklyReportHandler.HandleAsync(envelope.WeeklyReport, cancellationToken);
                break;
            case FeedEventEnvelope.EventOneofCase.None:
                _logger.LogWarning("Feed event envelope with empty payload received.");
                break;
            default:
                _logger.LogWarning("Unhandled feed event type {EventType}.", envelope.EventCase);
                break;
        }
    }
}
