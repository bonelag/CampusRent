using CampusRent.Server.Services;
using Xunit;
namespace CampusRent.Tests;
public class ServiceTests
{
    [Fact] public void SameLocationHasZeroDistance() => Assert.Equal(0, new HaversineService().CalculateDistanceKm(21, 105, 21, 105));
    [Fact] public void OneDegreeOnEquatorIs111Km() => Assert.InRange(new HaversineService().CalculateDistanceKm(0, 0, 0, 1), 111.18, 111.20);
    [Fact] public void AntipodesDoNotProduceNaN() => Assert.InRange(new HaversineService().CalculateDistanceKm(90, 0, -90, 0), 20015, 20016);
    [Fact] public void InvalidCoordinatesAreRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new HaversineService().CalculateDistanceKm(91, 0, 0, 0));
    [Fact] public void CostIncludesAllFees() => Assert.Equal(3774000, new LivingCostService().CalculateMonthlyCost(3200000, 3800, 30000, 100000, 50000));
    [Fact] public void ZeroUsageOnlyChargesFixedCosts() => Assert.Equal(1050, new LivingCostService().CalculateMonthlyCost(1000, 10, 10, 30, 20, 0, 0));
    [Fact] public void NegativeConsumptionIsRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new LivingCostService().CalculateMonthlyCost(1, 1, 1, 1, 1, -1));
    [Theory]
    [InlineData("Phòng sạch sẽ và thoáng", false)]
    [InlineData("ĐM phòng này", true)]
    [InlineData("FUCK", true)]
    [InlineData("admin thân thiện", false)]
    public void ProfanityUsesWordBoundaries(string text, bool expected) => Assert.Equal(expected, new ProfanityFilterService().CheckProfanity(text).IsFlagged);
}
