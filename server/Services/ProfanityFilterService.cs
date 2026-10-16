namespace CampusRent.Server.Services;

public interface IProfanityFilterService
{
    (bool IsFlagged, string? MatchedWord) CheckProfanity(string text);
}

public class ProfanityFilterService : IProfanityFilterService
{
    public (bool IsFlagged, string? MatchedWord) CheckProfanity(string text)
    {
        // TODO: Thành viên nhóm triển khai:
        // Kiểm tra danh sách từ ngữ cấm (Blacklist / Regex) trong bình luận review
        // Trả về (true, từ_bị_dính) nếu phát hiện từ cấm, ngược lại trả về (false, null)
        return (false, null);
    }
}
