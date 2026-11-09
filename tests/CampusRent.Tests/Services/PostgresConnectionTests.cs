using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace CampusRent.Tests;
public class PostgresConnectionTests
{
    [Fact]
    public void PostgresUrlsPreserveEscapedCredentialsAndUseVerifiedTls()
    {
        var value = new NpgsqlConnectionStringBuilder(PostgresConnection.Normalize("postgresql://owner:p%3Ba%22ss%3Aword@database.example.com/rental%20db?sslmode=verify-full"));
        Assert.Equal("p;a\"ss:word", value.Password);
        Assert.Equal("rental db", value.Database);
        Assert.Equal(SslMode.VerifyFull, value.SslMode);
        var remote = new NpgsqlConnectionStringBuilder(PostgresConnection.Normalize("Host=database.example.com;Database=rooms;Username=owner;Password=test"));
        Assert.Equal(SslMode.VerifyFull, remote.SslMode);
        var explicitLocal = new NpgsqlConnectionStringBuilder(PostgresConnection.Normalize("Host=localhost;Database=rooms;Username=owner;SSL Mode=Disable"));
        Assert.Equal(SslMode.Disable, explicitLocal.SslMode);
    }
}
