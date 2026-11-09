using Microsoft.AspNetCore.WebUtilities;
using Npgsql;

namespace CampusRent.Server.Services;

public static class PostgresConnection
{
    public static string Normalize(string connection)
    {
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Configure the PostgreSQL connection before starting the application.");
        var value = connection.Trim();
        NpgsqlConnectionStringBuilder builder;
        if (value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(value);
            var credentials = uri.UserInfo.Split(':', 2);
            builder = new()
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
                Username = Uri.UnescapeDataString(credentials[0]),
                Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : ""
            };
            var query = QueryHelpers.ParseQuery(uri.Query);
            if (query.TryGetValue("sslmode", out var ssl)) builder.SslMode = ssl.ToString().ToLowerInvariant() switch
            {
                "disable" => SslMode.Disable, "allow" => SslMode.Allow, "prefer" => SslMode.Prefer,
                "require" => SslMode.Require, "verify-ca" => SslMode.VerifyCA, "verify-full" => SslMode.VerifyFull,
                _ => throw new InvalidOperationException("Unsupported PostgreSQL SSL mode.")
            };
            else builder.SslMode = DefaultSslMode(builder.Host);
            if (query.TryGetValue("search_path", out var schema)) builder.SearchPath = schema.ToString();
        }
        else
        {
            builder = new(value);
            var supplied = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = value };
            if (!supplied.ContainsKey("SSL Mode") && !supplied.ContainsKey("SslMode")) builder.SslMode = DefaultSslMode(builder.Host);
        }
        return builder.ConnectionString;
    }

    private static SslMode DefaultSslMode(string? host) =>
        host is "localhost" or "127.0.0.1" or "::1" or "db" or "postgres" ? SslMode.Prefer : SslMode.VerifyFull;
}
