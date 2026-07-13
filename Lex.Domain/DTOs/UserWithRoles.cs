using Lex.Domain.Entities;

namespace Lex.Domain.DTOs;

public class UserWithRoles
{
    public User User { get; set; }
    public List<string> Roles { get; set; }
}