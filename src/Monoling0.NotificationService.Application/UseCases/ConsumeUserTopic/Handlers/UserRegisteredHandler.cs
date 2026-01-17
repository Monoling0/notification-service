using Monoling0.NotificationService.Application.UseCases.Common;
using Monoling0.NotificationService.Email.Models;
using Monoling0.NotificationService.Persistence.Repositories;
using User;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeUserTopic.Handlers;

public sealed class UserRegisteredHandler
{
    private readonly IUserEmailCacheRepository _userEmailCacheRepository;
    private readonly EmailNotificationService _emailNotificationService;

    public UserRegisteredHandler(
        IUserEmailCacheRepository userEmailCacheRepository,
        EmailNotificationService emailNotificationService)
    {
        _userEmailCacheRepository = userEmailCacheRepository;
        _emailNotificationService = emailNotificationService;
    }

    public async Task HandleAsync(UserRegisteredEvent userRegisteredEvent, CancellationToken cancellationToken)
    {
        var registeredAt = userRegisteredEvent.RegisteredAt.ToDateTime();

        await _userEmailCacheRepository.UpsertAsync(
            userRegisteredEvent.UserId,
            userRegisteredEvent.Email,
            registeredAt,
            cancellationToken);

        var model = new
        {
            userRegisteredEvent.UserId,
            userRegisteredEvent.Email,
            userRegisteredEvent.Name,
            RegisteredAt = registeredAt,
        };

        await _emailNotificationService.SendToUserAsync(
            EmailTemplates.Welcome,
            model,
            userRegisteredEvent.UserId,
            cancellationToken);
    }
}
