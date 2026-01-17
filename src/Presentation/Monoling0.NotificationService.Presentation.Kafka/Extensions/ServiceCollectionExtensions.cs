using Common;
using Course;
using Feed;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monoling0.NotificationService.Messaging.Kafka;
using Monoling0.NotificationService.Messaging.Kafka.Metadata;
using Monoling0.NotificationService.Messaging.Kafka.Models;
using Monoling0.NotificationService.Messaging.Kafka.Options;
using Monoling0.NotificationService.Observability;
using Monoling0.NotificationService.Presentation.Kafka.Batching;
using Monoling0.NotificationService.Presentation.Kafka.Dispatching;
using Monoling0.NotificationService.Presentation.Kafka.Metadata;
using Monoling0.NotificationService.Presentation.Kafka.Observability;
using Monoling0.NotificationService.Presentation.Kafka.Producers;
using Monoling0.NotificationService.Presentation.Kafka.Serialization;
using Progress;
using User;

namespace Monoling0.NotificationService.Presentation.Kafka.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentationKafkaOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<KafkaConsumerOptions>()
            .Bind(configuration.GetSection("Kafka:Consumer"))
            .ValidateOnStart();

        services.AddOptions<KafkaProducerOptions>()
            .Bind(configuration.GetSection("Kafka:Producer"))
            .ValidateOnStart();

        services.AddOptions<KafkaBatchingOptions>()
            .Bind(configuration.GetSection("Kafka:Batching"))
            .ValidateOnStart();

        services.AddOptions<KafkaDlqOptions>()
            .Bind(configuration.GetSection("Kafka:Dlq"))
            .ValidateOnStart();

        services.AddOptions<KafkaClientOptions>()
            .Bind(configuration.GetSection("Kafka:Client"))
            .ValidateOnStart();

        services.AddOptions<KafkaTopicsOptions>()
            .Bind(configuration.GetSection("Kafka:Topics"))
            .ValidateOnStart();

        services.AddOptions<KafkaProcessingOptions>()
            .Bind(configuration.GetSection("Kafka:Processing"))
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddPresentationKafka(this IServiceCollection services)
    {
        services.AddSingleton<ITracingContextAccessor, DefaultTracingContextAccessor>();
        services.AddSingleton(typeof(IKafkaSerializer<>), typeof(ProtobufKafkaSerializer<>));
        services.AddSingleton<IKafkaProducer, ConfluentKafkaProducer>();
        services.AddSingleton<IKafkaDlqProducer, ConfluentKafkaDlqProducer>();

        AddBatchSource<UserEventEnvelope>(
            services,
            options => options.UserEvents,
            envelope => envelope.Meta);
        AddBatchSource<UserRpcEventEnvelope>(
            services,
            options => options.UserEmailResponses,
            envelope => envelope.Meta);
        AddBatchSource<CourseEventEnvelope>(
            services,
            options => options.CourseEvents,
            envelope => envelope.Meta);
        AddBatchSource<ProgressEventEnvelope>(
            services,
            options => options.ProgressEvents,
            envelope => envelope.Meta);
        AddBatchSource<FeedEventEnvelope>(
            services,
            options => options.FeedEvents,
            envelope => envelope.Meta);

        services.AddHostedService<KafkaBatchDispatcher<UserEventEnvelope>>();
        services.AddHostedService<KafkaBatchDispatcher<UserRpcEventEnvelope>>();
        services.AddHostedService<KafkaBatchDispatcher<CourseEventEnvelope>>();
        services.AddHostedService<KafkaBatchDispatcher<ProgressEventEnvelope>>();
        services.AddHostedService<KafkaBatchDispatcher<FeedEventEnvelope>>();

        return services;
    }

    private static void AddBatchSource<T>(
        IServiceCollection services,
        Func<KafkaTopicsOptions, string> topicSelector,
        Func<T, EventMeta?> metadataSelector) where T : class, IMessage<T>, new()
    {
        services.AddSingleton<IKafkaEnvelopeMetadataAccessor<T>>(
            _ => new ProtobufEnvelopeMetadataAccessor<T>(metadataSelector));

        services.AddSingleton<IKafkaBatchSource<T>>(serviceProvider =>
        {
            KafkaTopicsOptions topics = serviceProvider
                .GetRequiredService<IOptions<KafkaTopicsOptions>>()
                .Value;
            var topic = new KafkaTopicName(topicSelector(topics));
            return ActivatorUtilities.CreateInstance<KafkaBatchSource<T>>(serviceProvider, topic);
        });
    }
}
