using CampusRent.Server.Models;

namespace CampusRent.Server.Services;

public sealed record RoomRatingStats(
    double AverageRating,
    int ReviewCount,
    double RecommendationScore,
    bool IsLowRated);

/// <summary>
/// Computes credibility-weighted review averages and Bayesian recommendation scores.
/// Student academic accounts (.edu / .edu.vn) carry higher weight to counter review bombing.
/// </summary>
public static class ReviewWeighting
{
    public const double VerifiedEduWeight = 2.5;
    public const double UnverifiedEduWeight = 1.5;
    public const double StandardWeight = 1.0;
    public const double LowRatingThreshold = 3.0;
    private const double PriorMean = 3.5;
    private const double PriorWeight = 2.0;

    public static double GetWeight(User? author)
    {
        if (author == null) return StandardWeight;
        var isEdu = author.IsEduVerified || User.IsEduDomain(author.Email);
        if (!isEdu) return StandardWeight;
        return author.IsEduVerified || author.IsEmailVerified ? VerifiedEduWeight : UnverifiedEduWeight;
    }

    public static RoomRatingStats Calculate(IEnumerable<Review> reviews)
    {
        var valid = reviews.Where(x => !x.IsFlagged).ToList();
        if (valid.Count == 0)
        {
            return new RoomRatingStats(0, 0, PriorMean, false);
        }

        double weightedSum = 0;
        double totalWeight = 0;
        foreach (var review in valid)
        {
            var weight = GetWeight(review.User);
            weightedSum += review.Rating * weight;
            totalWeight += weight;
        }

        var average = totalWeight > 0 ? Math.Round(weightedSum / totalWeight, 1) : 0;
        var bayesian = (weightedSum + PriorMean * PriorWeight) / (totalWeight + PriorWeight);
        return new RoomRatingStats(average, valid.Count, Math.Round(bayesian, 3), average < LowRatingThreshold);
    }
}
