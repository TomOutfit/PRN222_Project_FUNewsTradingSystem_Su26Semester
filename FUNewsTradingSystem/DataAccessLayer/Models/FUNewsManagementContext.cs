using Microsoft.EntityFrameworkCore;

namespace FUNewsTradingSystem_DataAccessLayer.Models;

public class FUNewsManagementContext : DbContext
{
    public FUNewsManagementContext(DbContextOptions<FUNewsManagementContext> options)
        : base(options)
    {
    }

    public DbSet<SystemAccount> SystemAccounts => Set<SystemAccount>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<NewsTag> NewsTags => Set<NewsTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- SystemAccount ---
        modelBuilder.Entity<SystemAccount>(entity =>
        {
            entity.ToTable("SystemAccount");
            entity.HasIndex(e => e.AccountEmail).IsUnique();
            entity.Property(e => e.AccountName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AccountEmail).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AccountPassword).HasMaxLength(500).IsRequired();
        });

        // --- Category (self-referencing FK) ---
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");
            entity.Property(e => e.CategoryName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CategoryDescription).HasMaxLength(500);
            entity.HasOne(c => c.ParentCategory)
                  .WithMany(c => c.ChildCategories)
                  .HasForeignKey(c => c.ParentCategoryID)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // --- Tag ---
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tag");
            entity.Property(e => e.TagName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.HasIndex(e => e.TagName).IsUnique();
        });

        // --- NewsArticle ---
        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.ToTable("NewsArticle");
            entity.Property(e => e.NewsTitle).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Headline).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.NewsContent).IsRequired();
            entity.Property(e => e.NewsSource).HasMaxLength(500);

            entity.HasOne(na => na.Category)
                  .WithMany(c => c.NewsArticles)
                  .HasForeignKey(na => na.CategoryID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(na => na.CreatedByAccount)
                  .WithMany()
                  .HasForeignKey(na => na.CreatedByID)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(na => na.UpdatedByAccount)
                  .WithMany()
                  .HasForeignKey(na => na.UpdatedByID)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // --- NewsTag (composite PK, junction table) ---
        modelBuilder.Entity<NewsTag>(entity =>
        {
            entity.ToTable("NewsTag");
            entity.HasKey(nt => new { nt.NewsArticleID, nt.TagID });

            entity.HasOne(nt => nt.NewsArticle)
                  .WithMany(na => na.NewsTagList)
                  .HasForeignKey(nt => nt.NewsArticleID)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(nt => nt.Tag)
                  .WithMany(t => t.NewsTags)
                  .HasForeignKey(nt => nt.TagID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

    }
}
