using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestTask.Core.Database.Entities;

namespace TestTask.Core.Database.Configurations;

public sealed class TaskEntityConfiguration : IEntityTypeConfiguration<TaskEntity>, ITaskDbConfiguration
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Priority).HasConversion<string>();
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion()
            .ValueGeneratedOnAddOrUpdate();
    }
}