namespace CampusRent.Server.BackgroundServices;

/// <summary>
/// Template BackgroundService: Tác vụ định kỳ tự động quét và ẩn tin phòng trọ quá hạn 30 ngày.
/// </summary>
public class AutoExpireRoomsWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoExpireRoomsWorker> _logger;

    public AutoExpireRoomsWorker(IServiceProvider serviceProvider, ILogger<AutoExpireRoomsWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[AutoExpireRoomsWorker] BackgroundService template sẵn sàng.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: Thành viên nhóm triển khai logic:
            // 1. Tạo Scope từ _serviceProvider để lấy CampusRentDbContext
            // 2. Truy vấn các phòng trọ có Status == RoomStatus.AVAILABLE và ExpiresAt < DateTime.UtcNow
            // 3. Cập nhật Status = RoomStatus.HIDDEN và lưu xuống database
            // 4. Bắt lỗi (try/catch) và log thông tin chi tiết

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
