using System.Net;
using System.Text;
using CampusRent.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusRent.Tests;
public class HaversineServiceTests
{
    [Fact]
    public void SameLocationHasZeroDistance() => Assert.Equal(0, new HaversineService().CalculateDistanceKm(21, 105, 21, 105));
    [Fact]
    public void OneDegreeOnEquatorIs111Km() => Assert.InRange(new HaversineService().CalculateDistanceKm(0, 0, 0, 1), 111.18, 111.20);
    [Fact]
    public void AntipodesDoNotProduceNaN() => Assert.InRange(new HaversineService().CalculateDistanceKm(90, 0, -90, 0), 20015, 20016);
    [Fact]
    public void InvalidCoordinatesAreRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new HaversineService().CalculateDistanceKm(91, 0, 0, 0));
}
