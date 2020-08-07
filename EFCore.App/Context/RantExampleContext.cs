using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using EFCore.App.Models;

namespace EFCore.App.Context
{
    public partial class RantExampleContext : DbContext
    {
        public RantExampleContext()
        {
        }

        public RantExampleContext(string connectionString):
            base(new DbContextOptionsBuilder<RantExampleContext>()
                .UseSqlServer(connectionString = connectionString)
                .Options)
        {
            
        }

        public RantExampleContext(DbContextOptions<RantExampleContext> options)
            : base(options)
        {
        }

        public virtual DbSet<CustomFieldValues> CustomFieldValues { get; set; }
        public virtual DbSet<TempReports> TempReports { get; set; }
        public virtual DbSet<TempReportsProject> TempReportsProject { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomFieldValues>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("CustomFieldValues_pk")
                    .IsClustered(false);

                entity.HasIndex(e => e.Id)
                    .HasName("CustomFieldValues_Id_uindex")
                    .IsUnique();

                entity.Property(e => e.CustomFieldId).HasColumnName("CustomField_Id");

                entity.Property(e => e.ProjectId).HasColumnName("Project_Id");
            });

            modelBuilder.Entity<TempReports>(entity =>
            {
                entity.HasKey(e => e.ResourceId)
                    .HasName("TempReports_pk")
                    .IsClustered(false);

                entity.HasIndex(e => e.ResourceId)
                    .HasName("TempReports_Resource_Id_uindex")
                    .IsUnique();

                entity.Property(e => e.ResourceId).HasColumnName("Resource_Id");
            });

            modelBuilder.Entity<TempReportsProject>(entity =>
            {
                entity.HasKey(e => e.ProjectId)
                    .HasName("TempReportsProject_pk")
                    .IsClustered(false);

                entity.HasIndex(e => e.ProjectId)
                    .HasName("TempReportsProject_Project_Id_uindex")
                    .IsUnique();

                entity.Property(e => e.ProjectId).HasColumnName("Project_Id");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
