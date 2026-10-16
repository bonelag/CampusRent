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
        // TODO: Thành viên nhóm triển khai:
        // Cài đặt công thức Haversine tính cự ly giữa 2 tọa độ GPS (lat1, lon1) và (lat2, lon2), làm tròn 2 chữ số thập phân.
        return 0.0;
    }
}
