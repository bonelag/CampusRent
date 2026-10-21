namespace CampusRent.Server.Services;

public interface IHaversineService
{
    double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2);
}

public class HaversineService : IHaversineService
{
    /// <summary>
    /// Tính khoảng cách giữa 2 tọa độ GPS (KM) theo công thức Haversine
    /// </summary>
    public double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        if (!double.IsFinite(lat1 + lat2 + lon1 + lon2) || Math.Abs(lat1) > 90 || Math.Abs(lat2) > 90 || Math.Abs(lon1) > 180 || Math.Abs(lon2) > 180)
            throw new ArgumentOutOfRangeException(nameof(lat1), "Invalid coordinates.");
        var radians = Math.PI / 180;
        var a = Math.Pow(Math.Sin((lat2 - lat1) * radians / 2), 2) + Math.Cos(lat1 * radians) * Math.Cos(lat2 * radians) * Math.Pow(Math.Sin((lon2 - lon1) * radians / 2), 2);
        return Math.Round(6371 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1))), 2);
    }
}

