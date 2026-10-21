namespace CampusRent.Server.Services;

public interface IProfanityFilterService
{
    (bool IsFlagged, string? MatchedWord) CheckProfanity(string text);
}

public class ProfanityFilterService : IProfanityFilterService
{
    public (bool IsFlagged, string? MatchedWord) CheckProfanity(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text.Normalize(),
            @"(?<!\p{L})(địt|đụ|đm|dm|vcl|cặc|lồn|fuck|shit)(?!\p{L})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        return (match.Success, match.Success ? match.Value : null);
    }
}

