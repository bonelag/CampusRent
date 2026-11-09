using System.Net;
using System.Text;
using CampusRent.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusRent.Tests;
public class LivingCostServiceTests
{
    [Fact]
    public void CostIncludesAllFees() => Assert.Equal(3774000, new LivingCostService().CalculateMonthlyCost(3200000, 3800, 30000, 100000, 50000));
    [Fact]
    public void ZeroUsageOnlyChargesFixedCosts() => Assert.Equal(1050, new LivingCostService().CalculateMonthlyCost(1000, 10, 10, 30, 20, 0, 0));
    [Fact]
    public void NegativeConsumptionIsRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new LivingCostService().CalculateMonthlyCost(1, 1, 1, 1, 1, -1));
}
