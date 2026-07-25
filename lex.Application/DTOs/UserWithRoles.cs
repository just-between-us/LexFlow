using Lex.Domain.Entities;

namespace lex.Application.DTOs;

public class UserWithRoles
{
    public User User { get; set; }
    public List<string> Roles { get; set; }
}