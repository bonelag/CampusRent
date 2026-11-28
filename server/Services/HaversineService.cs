namespace CampusRent.Server.Services;

public interface IHaversineService
{
    double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2);
}

public class HaversineService : IHaversineService
{
    private const double EarthRadiusKm = 6371.0;

    public double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var invalidLat = Math.Abs(lat1) > 90 || Math.Abs(lat2) > 90;
        var invalidLon = Math.Abs(lon1) > 180 || Math.Abs(lon2) > 180;
        if (!double.IsFinite(lat1 + lat2 + lon1 + lon2) || invalidLat || invalidLon)
        {
            throw new ArgumentOutOfRangeException(nameof(lat1), "Invalid coordinates.");
        }

        var toRadians = Math.PI / 180.0;
        var dLat = (lat2 - lat1) * toRadians;
        var dLon = (lon2 - lon1) * toRadians;
        var sinLat = Math.Sin(dLat / 2);
        var sinLon = Math.Sin(dLon / 2);

        var a = sinLat * sinLat + Math.Cos(lat1 * toRadians) * Math.Cos(lat2 * toRadians) * sinLon * sinLon;
        var c = 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
        return Math.Round(EarthRadiusKm * c, 2);
    }
}
