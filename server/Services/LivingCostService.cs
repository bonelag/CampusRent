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
    public decimal CalculateMonthlyCost(
        decimal basePrice,
        decimal electricPrice,
        decimal waterPrice,
        decimal internetPrice,
        decimal otherFee,
        decimal expectedKwh = 80,
        decimal expectedM3 = 4)
    {
        if (basePrice < 0 || electricPrice < 0 || waterPrice < 0 ||
            internetPrice < 0 || otherFee < 0 || expectedKwh < 0 || expectedM3 < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice));
        }

        var electricTotal = expectedKwh * electricPrice;
        var waterTotal = expectedM3 * waterPrice;
        return basePrice + electricTotal + waterTotal + internetPrice + otherFee;
    }
}
