namespace Monoling0.NotificationService.Observability;

public sealed record TracingContext(string? TraceId, string? SpanId, bool? Sampled);
