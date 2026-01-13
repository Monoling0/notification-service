namespace Monoling0.NotificationService.Persistence.Migrations;

public interface IDatabaseMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken);
}
