using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Xunit;

namespace CampusRent.Tests;

public class ReviewRankingServiceTests
{
    private readonly ReviewRankingService _service = new();

    [Fact]
    public void GetReviewWeight_NullUser_ReturnsRegularWeight()
    {
        var weight = _service.GetReviewWeight(null);
        Assert.Equal(ReviewRankingService.RegularUserWeight, weight);
    }

    [Fact]
    public void GetReviewWeight_RegularUser_ReturnsRegularWeight()
    {
        var user = new User
        {
            Email = "user@gmail.com",
            IsEduVerified = false
        };
        var weight = _service.GetReviewWeight(user);
        Assert.Equal(ReviewRankingService.RegularUserWeight, weight);
    }

    [Fact]
    public void GetReviewWeight_EduVerifiedUser_ReturnsVerifiedWeight()
    {
        var user = new User
        {
            Email = "student@gmail.com",
            IsEduVerified = true
        };
        var weight = _service.GetReviewWeight(user);
        Assert.Equal(ReviewRankingService.VerifiedStudentWeight, weight);
    }

    [Theory]
    [InlineData("sv@hust.edu.vn")]
    [InlineData("user@vnu.EDU.VN")]
    [InlineData("test@fpt.edu.vn")]
    public void GetReviewWeight_EduEmailDomain_ReturnsVerifiedWeight(string email)
    {
        var user = new User
        {
            Email = email,
            IsEduVerified = false
        };
        var weight = _service.GetReviewWeight(user);
        Assert.Equal(ReviewRankingService.VerifiedStudentWeight, weight);
    }

    [Fact]
    public void CalculateWeightedRating_NoReviews_ReturnsZero()
    {
        var rating = _service.CalculateWeightedRating(Enumerable.Empty<Review>());
        Assert.Equal(0, rating);
    }

    [Fact]
    public void CalculateWeightedRating_OnlyFlaggedReviews_ReturnsZero()
    {
        var reviews = new List<Review>
        {
            new() { Rating = 5, IsFlagged = true }
        };
        var rating = _service.CalculateWeightedRating(reviews);
        Assert.Equal(0, rating);
    }

    [Fact]
    public void CalculateWeightedRating_VerifiedStudentHasHigherWeight()
    {
        // 1 review from verified student (5 stars, weight 2.0)
        // 1 review from regular user (2 stars, weight 1.0)
        // Weighted = (5 * 2.0 + 2 * 1.0) / (2.0 + 1.0) = 12 / 3 = 4.0
        // (Unweighted would have been 3.5)
        var student = new User { Email = "sv@hust.edu.vn", IsEduVerified = true };
        var regular = new User { Email = "normal@gmail.com", IsEduVerified = false };

        var reviews = new List<Review>
        {
            new() { Rating = 5, User = student, IsFlagged = false },
            new() { Rating = 2, User = regular, IsFlagged = false }
        };

        var rating = _service.CalculateWeightedRating(reviews);
        Assert.Equal(4.0, rating);
    }

    [Fact]
    public void CalculateWeightedRating_RoundsTo1DecimalPlace()
    {
        // (4 * 2.0 + 3 * 1.0) / 3 = 11 / 3 = 3.6666... -> 3.7
        var student = new User { Email = "sv@neu.edu.vn" };
        var regular = new User { Email = "normal@gmail.com" };

        var reviews = new List<Review>
        {
            new() { Rating = 4, User = student, IsFlagged = false },
            new() { Rating = 3, User = regular, IsFlagged = false }
        };

        var rating = _service.CalculateWeightedRating(reviews);
        Assert.Equal(3.7, rating);
    }

    [Fact]
    public void EvaluateRoomDemotion_RoomWithLowAverageRating_IsDemoted()
    {
        var regular = new User { Email = "normal@gmail.com" };
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Reviews = new List<Review>
            {
                new() { Rating = 2, User = regular, IsFlagged = false },
                new() { Rating = 1, User = regular, IsFlagged = false }
            }
        };

        var (isDemoted, reason) = _service.EvaluateRoomDemotion(room);
        Assert.True(isDemoted);
        Assert.NotNull(reason);
        Assert.Contains("< 2.5", reason);
    }

    [Fact]
    public void EvaluateRoomDemotion_RoomWithHighNegativeRatio_IsDemoted()
    {
        // 2 reviews out of 3 are <= 2 stars (ratio = 66.7% >= 50%)
        // Weighted average: (5*1 + 2*1 + 1*1)/3 = 2.66... >= 2.5
        var regular = new User { Email = "normal@gmail.com" };
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Reviews = new List<Review>
            {
                new() { Rating = 5, User = regular, IsFlagged = false },
                new() { Rating = 2, User = regular, IsFlagged = false },
                new() { Rating = 1, User = regular, IsFlagged = false }
            }
        };

        var (isDemoted, reason) = _service.EvaluateRoomDemotion(room);
        Assert.True(isDemoted);
        Assert.NotNull(reason);
        Assert.Contains("50%", reason);
    }

    [Fact]
    public void EvaluateRoomDemotion_RoomWithGoodRating_IsNotDemoted()
    {
        var regular = new User { Email = "normal@gmail.com" };
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Reviews = new List<Review>
            {
                new() { Rating = 5, User = regular, IsFlagged = false },
                new() { Rating = 4, User = regular, IsFlagged = false }
            }
        };

        var (isDemoted, reason) = _service.EvaluateRoomDemotion(room);
        Assert.False(isDemoted);
        Assert.Null(reason);
    }

    [Fact]
    public void EvaluateRoomDemotion_LandlordWithLowAverage_DemotesRoom()
    {
        var landlordId = Guid.NewGuid();
        var regular = new User { Email = "normal@gmail.com" };
        var room = new Room
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            Reviews = new List<Review>() // Room itself has no reviews yet
        };

        var otherRoom = new Room { Id = Guid.NewGuid(), LandlordId = landlordId };
        var landlordReviews = new List<Review>
        {
            new() { Room = otherRoom, Rating = 1, User = regular, IsFlagged = false },
            new() { Room = otherRoom, Rating = 2, User = regular, IsFlagged = false }
        };

        var (isDemoted, reason) = _service.EvaluateRoomDemotion(room, landlordReviews);
        Assert.True(isDemoted);
        Assert.NotNull(reason);
        Assert.Contains("Chủ nhà", reason);
    }
}
