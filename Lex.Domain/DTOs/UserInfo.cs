namespace Lex.Application.DTOs;

public class UserInfo
{
    public required string UserId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsAuthenticated { get; set; }
    public List<string> Roles { get; set; } = new();
    public string FullName => $"{FirstName} {LastName}".Trim() ?? Email ?? UserId;

    public string Initials
    {
        get
        {
            if (string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName))
                return Email?.Substring(0, 1).ToUpper() ?? "U";
                
            var first = string.IsNullOrEmpty(FirstName) ? "" : FirstName[0].ToString();
            var last = string.IsNullOrEmpty(LastName) ? "" : LastName[0].ToString();
            return $"{first}{last}".ToUpper();
        }
    }
}