using CampusRent.Server.Models;

namespace CampusRent.Server.Services;

public interface IReviewRankingService
{
    double GetReviewWeight(User? user);
    double CalculateWeightedRating(IEnumerable<Review> reviews);
    (bool IsDemoted, string? Reason) EvaluateRoomDemotion(Room room, IEnumerable<Review>? landlordReviews = null);
}

public class ReviewRankingService : IReviewRankingService
{
    public const double VerifiedStudentWeight = 2.0;
    public const double RegularUserWeight = 1.0;
    public const double LowRatingThreshold = 2.5;
    public const double LowRatingRatioThreshold = 0.5;

    public double GetReviewWeight(User? user)
    {
        if (user == null) return RegularUserWeight;
        var isVerified = user.IsEduVerified ||
                         (!string.IsNullOrWhiteSpace(user.Email) && user.Email.EndsWith(".edu.vn", StringComparison.OrdinalIgnoreCase));
        return isVerified ? VerifiedStudentWeight : RegularUserWeight;
    }

    public double CalculateWeightedRating(IEnumerable<Review> reviews)
    {
        var activeReviews = reviews.Where(r => !r.IsFlagged).ToList();
        if (activeReviews.Count == 0) return 0;

        double weightedSum = 0;
        double totalWeight = 0;

        foreach (var r in activeReviews)
        {
            var weight = GetReviewWeight(r.User);
            weightedSum += r.Rating * weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0) return 0;
        return Math.Round(weightedSum / totalWeight, 1);
    }

    public (bool IsDemoted, string? Reason) EvaluateRoomDemotion(Room room, IEnumerable<Review>? landlordReviews = null)
    {
        var roomReviews = (room.Reviews ?? Enumerable.Empty<Review>()).Where(r => !r.IsFlagged).ToList();
        bool roomDemoted = false;
        string? roomReason = null;

        if (roomReviews.Count >= 1)
        {
            var roomAvg = CalculateWeightedRating(roomReviews);
            var lowCount = roomReviews.Count(r => r.Rating <= 2);
            var lowRatio = (double)lowCount / roomReviews.Count;

            if (roomAvg < LowRatingThreshold)
            {
                roomDemoted = true;
                roomReason = "Phòng có điểm đánh giá trung bình thấp (< 2.5 sao)";
            }
            else if (lowRatio >= LowRatingRatioThreshold && roomReviews.Count >= 2)
            {
                roomDemoted = true;
                roomReason = "Phòng có trên 50% đánh giá tiêu cực (1-2 sao)";
            }
        }

        bool landlordDemoted = false;
        string? landlordReason = null;

        if (landlordReviews != null)
        {
            var activeLandlordReviews = landlordReviews.Where(r => !r.IsFlagged).ToList();
            if (activeLandlordReviews.Count >= 2)
            {
                var landlordAvg = CalculateWeightedRating(activeLandlordReviews);
                var landlordLowCount = activeLandlordReviews.Count(r => r.Rating <= 2);
                var landlordLowRatio = (double)landlordLowCount / activeLandlordReviews.Count;

                if (landlordAvg < LowRatingThreshold)
                {
                    landlordDemoted = true;
                    landlordReason = "Chủ nhà có điểm đánh giá trung bình thấp (< 2.5 sao)";
                }
                else if (landlordLowRatio >= LowRatingRatioThreshold)
                {
                    landlordDemoted = true;
                    landlordReason = "Chủ nhà có tỷ lệ đánh giá tiêu cực cao (>= 50%)";
                }
            }
        }

        if (roomDemoted && landlordDemoted)
        {
            return (true, $"{roomReason}; {landlordReason}");
        }
        if (roomDemoted)
        {
            return (true, roomReason);
        }
        if (landlordDemoted)
        {
            return (true, landlordReason);
        }

        return (false, null);
    }
}
