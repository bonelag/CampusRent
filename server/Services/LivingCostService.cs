namespace CampusRent.Server.Services;

public interface ILivingCostService
{
    decimal CalculateMonthlyCost(
        decimal basePrice,
        decimal electricPrice,
        decimal waterPrice,
        decimal internetPrice,
        decimal otherFee,
        decimal expectedKwh = 80,
        decimal expectedM3 = 4);
}

public class LivingCostService : ILivingCostService
{
    /// <summary>
    /// Dự toán chi phí sinh hoạt hàng tháng
    /// </summary>
    public decimal CalculateMonthlyCost(
        decimal basePrice,
        decimal electricPrice,
        decimal waterPrice,
        decimal internetPrice,
        decimal otherFee,
        decimal expectedKwh = 80,
        decimal expectedM3 = 4)
    {
        if (new[] { basePrice, electricPrice, waterPrice, internetPrice, otherFee, expectedKwh, expectedM3 }.Any(v => v < 0))
            throw new ArgumentOutOfRangeException(nameof(basePrice));
        return basePrice + expectedKwh * electricPrice + expectedM3 * waterPrice + internetPrice + otherFee;
    }
}

