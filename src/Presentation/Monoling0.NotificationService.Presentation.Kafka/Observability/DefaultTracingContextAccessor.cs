using Monoling0.NotificationService.Observability;
using System.Text;

namespace Monoling0.NotificationService.Presentation.Kafka.Observability;

public sealed class DefaultTracingContextAccessor : ITracingContextAccessor
{
    private const string TraceIdHeader = "trace_id";
    private const string SpanIdHeader = "span_id";
    private const string SampledHeader = "sampled";

    public void Inject(TracingContext context, IHeaderCarrier carrier)
    {
        if (!string.IsNullOrWhiteSpace(context.TraceId))
            carrier.SetHeader(TraceIdHeader, Encoding.UTF8.GetBytes(context.TraceId));

        if (!string.IsNullOrWhiteSpace(context.SpanId))
            carrier.SetHeader(SpanIdHeader, Encoding.UTF8.GetBytes(context.SpanId));

        if (context.Sampled.HasValue)
            carrier.SetHeader(SampledHeader, Encoding.UTF8.GetBytes(context.Sampled.Value ? "1" : "0"));
    }

    public TracingContext Extract(IHeaderCarrier carrier)
    {
        string? traceId = TryGetUtf8(carrier, TraceIdHeader);
        string? spanId = TryGetUtf8(carrier, SpanIdHeader);
        bool? sampled = TryGetSampled(carrier);

        return new TracingContext(traceId, spanId, sampled);
    }

    private static string? TryGetUtf8(IHeaderCarrier carrier, string key)
    {
        return carrier.TryGetHeader(key, out byte[]? value)
            ? Encoding.UTF8.GetString(value)
            : null;
    }

    private static bool? TryGetSampled(IHeaderCarrier carrier)
    {
        if (!carrier.TryGetHeader(SampledHeader, out byte[]? value))
            return null;

        string text = Encoding.UTF8.GetString(value);

        return text switch
        {
            "1" => true,
            "0" => false,
            _ => bool.TryParse(text, out bool parsed) ? parsed : null,
        };
    }
}
