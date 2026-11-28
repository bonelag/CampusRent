using CampusRent.Server.Models;

namespace CampusRent.Server.Services;

public interface IReviewRankingService
{
    double GetReviewWeight(User? user);
    double CalculateWeightedRating(IEnumerable<Review> reviews);
    double CalculateRecommendationScore(IEnumerable<Review> reviews);
    (bool IsDemoted, string? Reason) EvaluateRoomDemotion(Room room, IEnumerable<Review>? landlordReviews = null);
}

public class ReviewRankingService : IReviewRankingService
{
    public const double VerifiedStudentWeight = 2.0;
    public const double RegularUserWeight = 1.0;
    public const double LowRatingThreshold = 2.5;
    public const double LowRatingRatioThreshold = 0.5;
    private const double DefaultRecommendationScore = 3.5;

    public double GetReviewWeight(User? user)
    {
        if (user == null)
        {
            return RegularUserWeight;
        }

        var isEduStudent = user.IsEduVerified || User.IsEduDomain(user.Email);
        return isEduStudent ? VerifiedStudentWeight : RegularUserWeight;
    }

    public double CalculateWeightedRating(IEnumerable<Review> reviews)
    {
        var activeReviews = reviews.Where(r => !r.IsFlagged).ToList();
        if (activeReviews.Count == 0)
        {
            return 0;
        }

        double weightedSum = 0;
        double totalWeight = 0;

        foreach (var review in activeReviews)
        {
            var weight = GetReviewWeight(review.User);
            weightedSum += review.Rating * weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            return 0;
        }

        return Math.Round(weightedSum / totalWeight, 1);
    }

    public double CalculateRecommendationScore(IEnumerable<Review> reviews)
    {
        var activeReviews = reviews.Where(r => !r.IsFlagged).ToList();
        if (activeReviews.Count == 0)
        {
            return DefaultRecommendationScore;
        }

        return CalculateWeightedRating(activeReviews);
    }

    public (bool IsDemoted, string? Reason) EvaluateRoomDemotion(Room room, IEnumerable<Review>? landlordReviews = null)
    {
        var roomReviews = (room.Reviews ?? Enumerable.Empty<Review>()).Where(r => !r.IsFlagged).ToList();
        var roomReason = CheckLowRatingReason(roomReviews, minReviewsForRatio: 2, isLandlord: false);

        string? landlordReason = null;
        if (landlordReviews != null)
        {
            var activeLandlordReviews = landlordReviews.Where(r => !r.IsFlagged).ToList();
            if (activeLandlordReviews.Count >= 2)
            {
                landlordReason = CheckLowRatingReason(activeLandlordReviews, minReviewsForRatio: 2, isLandlord: true);
            }
        }

        if (roomReason != null && landlordReason != null)
        {
            return (true, $"{roomReason}; {landlordReason}");
        }

        if (roomReason != null)
        {
            return (true, roomReason);
        }

        if (landlordReason != null)
        {
            return (true, landlordReason);
        }

        return (false, null);
    }

    private string? CheckLowRatingReason(List<Review> reviews, int minReviewsForRatio, bool isLandlord)
    {
        if (reviews.Count == 0)
        {
            return null;
        }

        var average = CalculateWeightedRating(reviews);
        var lowCount = reviews.Count(r => r.Rating <= 2);
        var lowRatio = (double)lowCount / reviews.Count;
        var subject = isLandlord ? "Chủ nhà" : "Phòng";

        if (average < LowRatingThreshold)
        {
            return $"{subject} có điểm đánh giá trung bình thấp (< 2.5 sao)";
        }

        if (reviews.Count >= minReviewsForRatio && lowRatio >= LowRatingRatioThreshold)
        {
            return isLandlord
                ? "Chủ nhà có tỷ lệ đánh giá tiêu cực cao (>= 50%)"
                : "Phòng có trên 50% đánh giá tiêu cực (1-2 sao)";
        }

        return null;
    }
}
