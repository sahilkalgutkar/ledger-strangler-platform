using LegacyMonolith.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyMonolith.Data;

public class LegacyDbContext : DbContext
{
    public LegacyDbContext(DbContextOptions<LegacyDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Statement> Statements => Set<Statement>();
}
