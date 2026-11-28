using System.Text.RegularExpressions;

namespace CampusRent.Server.Services;

public interface IProfanityFilterService
{
    (bool IsFlagged, string? MatchedWord) CheckProfanity(string text);
}

public class ProfanityFilterService : IProfanityFilterService
{
    private const string BlockedWordsPattern = @"(?<!\p{L})(địt|đụ|đm|dm|vcl|cặc|lồn|fuck|shit)(?!\p{L})";

    public (bool IsFlagged, string? MatchedWord) CheckProfanity(string text)
    {
        var match = Regex.Match(
            text.Normalize(),
            BlockedWordsPattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        return (match.Success, match.Success ? match.Value : null);
    }
}
