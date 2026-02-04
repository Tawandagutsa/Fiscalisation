using System.Data;
using Microsoft.Data.SqlClient;
using FiscalisationService.Models;

namespace FiscalisationService.Services;

public sealed class SqlRepository
{
    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetPendingAsync(ServiceConfig config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            return Array.Empty<Dictionary<string, object?>>();
        }

        var results = new List<Dictionary<string, object?>>();
        var whereClause = config.EffectiveWhereClause();
        var query = $"SELECT TOP (@BatchSize) * FROM {config.TableName} WHERE {whereClause} ORDER BY [ID]";

        await using var connection = new SqlConnection(config.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add(new SqlParameter("@BatchSize", SqlDbType.Int) { Value = config.BatchSize });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                record[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
            }

            results.Add(record);
        }

        return results;
    }

    public async Task UpdateResponseAsync(ServiceConfig config, int id, FiscalResponse response, string? rawResponse, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            return;
        }

        var updates = new List<(string Column, object? Value)>();

        AddUpdate(updates, config.ResponseColumns.VerificationCode, response.VerificationCode);
        AddUpdate(updates, config.ResponseColumns.QrUrl, response.QrUrl);
        AddUpdate(updates, config.ResponseColumns.FiscalisationStatus, response.FiscalisationStatus);
        AddUpdate(updates, config.ResponseColumns.DReceiptNumber, response.DReceiptNumber);
        AddUpdate(updates, config.ResponseColumns.InvoiceDate, response.InvoiceDate);
        AddUpdate(updates, config.ResponseColumns.DeviceId, response.DeviceId);
        AddUpdate(updates, config.ResponseColumns.ErrorMessage, null);
        AddUpdate(updates, config.ResponseColumns.FullResponse, rawResponse);
        AddUpdate(updates, config.LastSuccessAtColumn, DateTimeOffset.UtcNow);
        AddUpdate(updates, config.RetryCountColumn, 0);

        if (updates.Count == 0)
        {
            return;
        }

        var setClause = string.Join(", ", updates.Select((u, index) => $"[{u.Column}] = @p{index}"));
        var sql = $"UPDATE {config.TableName} SET {setClause} WHERE [ID] = @id";

        await using var connection = new SqlConnection(config.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });

        for (var i = 0; i < updates.Count; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", updates[i].Value ?? DBNull.Value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateFailureAsync(ServiceConfig config, int id, string errorMessage, string? rawResponse, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(config.ResponseColumns.ErrorMessage))
        {
            return;
        }

        var updates = new List<(string Column, object? Value)>
        {
            (config.ResponseColumns.ErrorMessage, errorMessage)
        };

        if (!string.IsNullOrWhiteSpace(config.ResponseColumns.FullResponse))
        {
            updates.Add((config.ResponseColumns.FullResponse, rawResponse));
        }

        AddUpdate(updates, config.LastAttemptAtColumn, DateTimeOffset.UtcNow);

        var setClause = string.Join(", ", updates.Select((u, index) => $"[{u.Column}] = @p{index}"));
        var sql = $"UPDATE {config.TableName} SET {setClause} WHERE [ID] = @id";

        await using var connection = new SqlConnection(config.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
        for (var i = 0; i < updates.Count; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", updates[i].Value ?? DBNull.Value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateTimeoutStatusAsync(ServiceConfig config, int id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionString) || string.IsNullOrWhiteSpace(config.StatusColumn))
        {
            return;
        }

        var sql = $"UPDATE {config.TableName} SET [{config.StatusColumn}] = @status WHERE [ID] = @id";

        await using var connection = new SqlConnection(config.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
        command.Parameters.AddWithValue("@status", config.TimeoutStatusValue);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkInProgressAsync(ServiceConfig config, int id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionString) || string.IsNullOrWhiteSpace(config.StatusColumn))
        {
            return;
        }

        var updates = new List<(string Column, object? Value)>
        {
            (config.StatusColumn, config.InProgressStatusValue)
        };

        AddUpdate(updates, config.LastAttemptAtColumn, DateTimeOffset.UtcNow);

        var setClause = string.Join(", ", updates.Select((u, index) => $"[{u.Column}] = @p{index}"));
        var sql = $"UPDATE {config.TableName} SET {setClause} WHERE [ID] = @id";

        await using var connection = new SqlConnection(config.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
        for (var i = 0; i < updates.Count; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", updates[i].Value ?? DBNull.Value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task IncrementRetryAsync(ServiceConfig config, int id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionString) || string.IsNullOrWhiteSpace(config.RetryCountColumn))
        {
            return;
        }

        var sql = $"UPDATE {config.TableName} SET [{config.RetryCountColumn}] = ISNULL([{config.RetryCountColumn}], 0) + 1 WHERE [ID] = @id";

        await using var connection = new SqlConnection(config.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(ServiceConfig config, int id, string status, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionString) || string.IsNullOrWhiteSpace(config.StatusColumn))
        {
            return;
        }

        var sql = $"UPDATE {config.TableName} SET [{config.StatusColumn}] = @status WHERE [ID] = @id";

        await using var connection = new SqlConnection(config.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
        command.Parameters.AddWithValue("@status", status);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddUpdate(List<(string Column, object? Value)> updates, string column, object? value)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            return;
        }

        updates.Add((column, value));
    }
}
