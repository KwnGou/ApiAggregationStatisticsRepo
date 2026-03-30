using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ApiAggregation.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Statistic> Statistics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Statistic>(entity =>
        {
            entity.HasIndex(e => new { e.Api, e.RequestDate }, "UniqueAPIDateBuckets").IsUnique();

            entity.Property(e => e.Api)
                .HasMaxLength(100)
                .HasColumnName("API");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
