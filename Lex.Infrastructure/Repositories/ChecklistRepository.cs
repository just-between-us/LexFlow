using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Lex.Infrastructure.Data;

namespace Lex.Infrastructure.Repositories;

public class ChecklistRepository : Repository<Checklist>
{
    public ChecklistRepository(AppDbContext context) : base(context)
    {
    }
}