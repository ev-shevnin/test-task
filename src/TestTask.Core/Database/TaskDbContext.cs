using Microsoft.EntityFrameworkCore;
using TestTask.Core.Database.Configurations;
using TestTask.Core.Database.Entities;

namespace TestTask.Core.Database;

public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskEntity> Tasks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TaskDbContext).Assembly,
            t => typeof(ITaskDbConfiguration).IsAssignableFrom(t));
        base.OnModelCreating(modelBuilder);
    }
}
