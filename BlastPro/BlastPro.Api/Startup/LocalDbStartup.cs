using Microsoft.Data.SqlClient;

namespace BlastPro.Api.Startup;

internal static class LocalDbStartup
{
    private const string PipeDirectory = @"\\.\pipe\";

    public static async Task EnsureAvailableAsync(string connectionString, ILogger logger)
    {
        if (!OperatingSystem.IsWindows() || !TryGetInstanceName(connectionString, out var instanceName))
            return;

        try
        {
            await OpenMasterAsync(connectionString);
            return;
        }
        catch (SqlException exception) when (IsLocalDbStartFailure(exception))
        {
            logger.LogWarning("LocalDB failed to start. Checking for a stale {InstanceName} process.", instanceName);
            if (!await ShutDownStaleInstanceAsync(instanceName, logger))
                throw new InvalidOperationException(
                    $"LocalDB instance '{instanceName}' could not start. Close other SQL tools and restart the LocalDB instance.",
                    exception);
        }

        // A graceful SQL shutdown can take a few seconds to release the master files.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            await Task.Delay(500);
            try
            {
                await OpenMasterAsync(connectionString);
                logger.LogInformation("Recovered LocalDB instance {InstanceName}.", instanceName);
                return;
            }
            catch (SqlException exception) when (IsLocalDbStartFailure(exception) && attempt < 9)
            {
                logger.LogDebug(exception, "Waiting for LocalDB to restart.");
            }
        }
    }

    private static bool TryGetInstanceName(string connectionString, out string instanceName)
    {
        var source = new SqlConnectionStringBuilder(connectionString).DataSource;
        const string prefix = @"(localdb)\";
        instanceName = source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? source[prefix.Length..]
            : string.Empty;
        return instanceName.Length > 0 && instanceName.IndexOfAny(['\\', '/']) < 0;
    }

    private static bool IsLocalDbStartFailure(SqlException exception) =>
        exception.Message.Contains("Local Database Runtime error", StringComparison.OrdinalIgnoreCase) &&
        exception.Message.Contains("SQL Server process failed to start", StringComparison.OrdinalIgnoreCase);

    private static async Task OpenMasterAsync(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = 5,
            Pooling = false
        };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
    }

    private static async Task<bool> ShutDownStaleInstanceAsync(string instanceName, ILogger logger)
    {
        var expectedMaster = Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Microsoft SQL Server Local DB", "Instances", instanceName, "master.mdf"));

        foreach (var pipe in Directory.EnumerateFiles(PipeDirectory)
                     .Where(path => path[PipeDirectory.Length..].StartsWith("LOCALDB#", StringComparison.OrdinalIgnoreCase)
                         && path.EndsWith(@"\tsql\query", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = "np:" + pipe,
                    InitialCatalog = "master",
                    IntegratedSecurity = true,
                    Encrypt = false,
                    Pooling = false,
                    ConnectTimeout = 3
                };
                await using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();

                await using var masterFile = new SqlCommand(
                    "SELECT physical_name FROM sys.master_files WHERE database_id = 1 AND file_id = 1", connection);
                var actualMaster = (string?)await masterFile.ExecuteScalarAsync();
                if (actualMaster is null ||
                    !Path.GetFullPath(actualMaster).Equals(expectedMaster, StringComparison.OrdinalIgnoreCase))
                    continue;

                await using var sessions = new SqlCommand(
                    "SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1 AND session_id <> @@SPID", connection);
                if ((int)(await sessions.ExecuteScalarAsync())! > 0)
                    throw new InvalidOperationException(
                        $"LocalDB instance '{instanceName}' has active connections. Close those applications before restarting it.");

                logger.LogWarning("Shutting down stale LocalDB instance {InstanceName} gracefully.", instanceName);
                try
                {
                    await using var shutdown = new SqlCommand("SHUTDOWN", connection) { CommandTimeout = 10 };
                    await shutdown.ExecuteNonQueryAsync();
                }
                catch (SqlException)
                {
                    // SHUTDOWN closes the connection before SqlClient receives a response.
                }

                SqlConnection.ClearAllPools();
                return true;
            }
            catch (SqlException exception)
            {
                logger.LogDebug(exception, "Could not connect to LocalDB pipe {Pipe}.", pipe);
            }
        }

        return false;
    }
}
