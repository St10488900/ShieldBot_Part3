using Microsoft.EntityFrameworkCore;

namespace CyberAwarenessBot
{
    /// <summary>
    /// Entity Framework Core DbContext configured for SQLite.
    /// Stores tasks and activity log entries in database.db.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public DbSet<CyberTask> Tasks { get; set; }
        public DbSet<CyberLog>  Logs  { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=database.db");
            optionsBuilder.UseLazyLoadingProxies();
        }
    }
}
