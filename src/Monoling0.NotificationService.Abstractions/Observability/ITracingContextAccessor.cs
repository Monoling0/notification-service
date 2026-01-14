namespace Monoling0.NotificationService.Observability;

public interface ITracingContextAccessor
{
    void Inject(TracingContext context, IHeaderCarrier carrier);

    TracingContext Extract(IHeaderCarrier carrier);
}
