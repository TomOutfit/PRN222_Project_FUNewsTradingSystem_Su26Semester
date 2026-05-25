using Microsoft.EntityFrameworkCore;
using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_DataAccessLayer.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SystemAccount> SystemAccounts { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<NewsArticle> NewsArticles { get; set; }
    public DbSet<NewsTag> NewsTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- SystemAccount ---
        modelBuilder.Entity<SystemAccount>(entity =>
        {
            entity.HasKey(e => e.AccountId);
            entity.Property(e => e.AccountName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AccountEmail).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AccountPassword).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AccountRole).IsRequired();
            entity.HasIndex(e => e.AccountEmail).IsUnique();
        });

        // --- Category ---
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CategoryDescription).HasMaxLength(500);

            entity.HasOne(e => e.ParentCategory)
                .WithMany(e => e.ChildCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Tag ---
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId);
            entity.Property(e => e.TagName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(200);
            entity.HasIndex(e => e.TagName).IsUnique();
        });

        // --- NewsArticle ---
        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.HasKey(e => e.NewsArticleId);
            entity.Property(e => e.NewsArticleId).HasMaxLength(20);
            entity.Property(e => e.NewsTitle).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NewsContent).IsRequired();
            entity.Property(e => e.NewsSource).HasMaxLength(100);
            entity.Property(e => e.NewsImage).HasMaxLength(500);

            entity.HasOne(e => e.Category)
                .WithMany(e => e.NewsArticles)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedByAccount)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.UpdatedByAccount)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- NewsTag (Composite PK) ---
        modelBuilder.Entity<NewsTag>(entity =>
        {
            entity.HasKey(e => new { e.NewsArticleId, e.TagId });

            entity.HasOne(e => e.NewsArticle)
                .WithMany(e => e.NewsTags)
                .HasForeignKey(e => e.NewsArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                .WithMany(e => e.NewsTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Seed Data: Admin Account ---
        modelBuilder.Entity<SystemAccount>().HasData(
            new SystemAccount
            {
                AccountId = 1,
                AccountName = "System Admin",
                AccountEmail = "admin@FUNewsTradingSystem.org",
                AccountRole = 1,
                AccountPassword = "@@abc123@@"
            }
        );
    }
}
