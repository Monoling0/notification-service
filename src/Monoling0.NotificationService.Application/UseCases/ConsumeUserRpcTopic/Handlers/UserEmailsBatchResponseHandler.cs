using Google.Protobuf.WellKnownTypes;
using User;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeUserRpcTopic.Handlers;

public sealed class UserEmailsBatchResponseHandler
{
    private readonly UserEmailResponseHandler _userEmailResponseHandler;

    public UserEmailsBatchResponseHandler(UserEmailResponseHandler userEmailResponseHandler)
    {
        _userEmailResponseHandler = userEmailResponseHandler;
    }

    public async Task HandleAsync(UserEmailsResponseEvent responseEvent, CancellationToken cancellationToken)
    {
        if (responseEvent.Items.Count == 0)
            return;

        var respondedAt = responseEvent.RespondedAt.ToDateTime();
        var respondedAtTimestamp = Timestamp.FromDateTime(respondedAt);

        foreach (UserEmailResponseItem item in responseEvent.Items)
        {
            var singleResponse = new UserEmailResponseEvent
            {
                CorrelationId = item.CorrelationId,
                UserId = item.UserId,
                Email = item.Email,
                RespondedAt = respondedAtTimestamp,
            };

            if (!string.IsNullOrWhiteSpace(item.Error))
                singleResponse.Error = item.Error;

            await _userEmailResponseHandler.HandleAsync(singleResponse, cancellationToken);
        }
    }
}
