using Npgsql;
using NpgsqlTypes;

namespace Monoling0.NotificationService.Persistence.Extensions;

public static class DatabaseExtensions
{
    public static NpgsqlCommand NewCommand(this NpgsqlDataSource dataSource, string sql)
    {
        return dataSource.CreateCommand(sql);
    }

    public static NpgsqlCommand With(this NpgsqlCommand command, Action<NpgsqlCommand> action)
    {
        action(command);

        return command;
    }

    public static NpgsqlParameter AddParameter<T>(
        this NpgsqlCommand command,
        string name,
        T value,
        NpgsqlDbType? dbType = null)
    {
        var parameter = new NpgsqlParameter<T>
        {
            ParameterName = name,
            Value = value,
        };

        if (dbType.HasValue)
            parameter.NpgsqlDbType = dbType.Value;

        return command.Parameters.Add(parameter);
    }

    public static NpgsqlParameter AddArrayOfParameters<T>(
        this NpgsqlCommand command,
        string name,
        IEnumerable<T>? values,
        NpgsqlDbType? dbType = null)
    {
        T[] array = values?.ToArray() ?? [];

        var parameter = new NpgsqlParameter
        {
            ParameterName = name,
            Value = array,
        };

        if (dbType.HasValue)
        {
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            parameter.NpgsqlDbType = dbType.Value | NpgsqlDbType.Array;
        }

        return command.Parameters.Add(parameter);
    }

    public static NpgsqlParameter AddJsonbParameter(this NpgsqlCommand cmd, string name, string json)
    {
        return cmd.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb) { Value = json });
    }

    public static Task<int> AsNonQueryAsync(
        this NpgsqlCommand command,
        CancellationToken cancellationToken = default)
    {
        return command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task<T?> AsScalarAsync<T>(
        this NpgsqlCommand command,
        CancellationToken cancellationToken = default)
    {
        object? result = await command.ExecuteScalarAsync(cancellationToken);

        return result is null or DBNull ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    public static async Task<T?> ReadOnce<T>(
        this NpgsqlCommand command,
        Func<NpgsqlDataReader, T> map,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? map(reader) : default;
    }

    public static async Task<List<T>> ReadMany<T>(
        this NpgsqlCommand command,
        Func<NpgsqlDataReader, T> map,
        CancellationToken cancellationToken = default)
    {
        var list = new List<T>();

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            list.Add(map(reader));

        return list;
    }
}
