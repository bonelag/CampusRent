using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using CampusRent.Server.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace CampusRent.Tests;
public class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CAMPUSRENT_TEST_POSTGRES")))
            Skip = "Set CAMPUSRENT_TEST_POSTGRES to run PostgreSQL migrations and persistence checks (enabled in CI).";
    }
}
