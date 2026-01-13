using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monoling0.NotificationService.Options;
using Npgsql;

namespace Monoling0.NotificationService.Persistence.Database;

public sealed class NotificationDatabaseDataSource : IAsyncDisposable
{
    public NotificationDatabaseDataSource(
        IOptions<PostgresOptions> postgresOptions,
        ILoggerFactory loggerFactory)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(postgresOptions.Value.ConnectionString);

        dataSourceBuilder.UseLoggerFactory(loggerFactory);

        DataSource = dataSourceBuilder.Build();
    }

    public NpgsqlDataSource DataSource { get; }

    public ValueTask DisposeAsync()
    {
        return DataSource.DisposeAsync();
    }
}
