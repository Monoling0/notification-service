using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monoling0.NotificationService.Options;
using Monoling0.NotificationService.Persistence.Database;
using Monoling0.NotificationService.Persistence.Repositories;
using Monoling0.NotificationService.Persistence.Services;
using Monoling0.NotificationService.Persistence.Transactions;

namespace Monoling0.NotificationService.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<NotificationDatabaseDataSource>();
        serviceCollection.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

        serviceCollection.AddScoped<ICoursesCacheRepository, CourseCacheRepository>();
        serviceCollection.AddScoped<IEmailOutboxRepository, EmailOutboxRepository>();
        serviceCollection.AddScoped<IFollowersCacheRepository, FollowersCacheRepository>();
        serviceCollection.AddScoped<IInboxRepository, InboxRepository>();
        serviceCollection.AddScoped<IPendingEmailRequestRepository, PendingEmailRequestRepository>();
        serviceCollection.AddScoped<IUserEmailCacheRepository, UserEmailCacheRepository>();

        serviceCollection
            .AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(sp =>
                    sp.GetRequiredService<IOptions<PostgresOptions>>().Value.ConnectionString)
                .WithMigrationsIn(typeof(IAssemblyMaker).Assembly));

        serviceCollection.AddHostedService<MigrationService>();

        return serviceCollection;
    }

    public static IServiceCollection AddPersistenceOptions(
        this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection("Persistence:Postgres"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return serviceCollection;
    }
}
