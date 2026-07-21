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
    public DbSet<SavedReport> SavedReports => Set<SavedReport>();
    public DbSet<TagCategoryMap> TagCategoryMaps => Set<TagCategoryMap>();

    /// <summary>
    /// Configures the EF Core model: table names, column constraints, relationships,
    /// unique/indexes, delete behaviours, and seed data for <see cref="SystemAccount"/>,
    /// <see cref="Category"/>, and <see cref="Tag"/>.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- SystemAccount ---
        // Table + unique index on email (no two accounts share an email).
        // All string columns have explicit max lengths and are required where appropriate.
        modelBuilder.Entity<SystemAccount>(entity =>
        {
            entity.ToTable("SystemAccount");
            entity.HasIndex(e => e.AccountEmail).IsUnique();
            entity.Property(e => e.AccountName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AccountEmail).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AccountPassword).HasMaxLength(500).IsRequired();
        });

        // --- Category (self-referencing FK) ---
        // Supports a parent-child hierarchy: a category may have one parent and many children.
        // ParentCategoryID is nullable; NoAction prevents cycles on delete.
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
        // TagName is unique and stored normalised to uppercase; duplicate tickers are rejected at DB level.
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tag");
            entity.Property(e => e.TagName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.HasIndex(e => e.TagName).IsUnique();
        });

        // --- NewsArticle ---
        // Three foreign keys: Category (Restrict to prevent orphan deletion),
        // CreatedByAccount (SetNull so a deleted creator leaves a readable article),
        // UpdatedByAccount (NoAction so a deleted updater does not block article deletion).
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
        // Composite PK on (NewsArticleID, TagID) prevents duplicate pairings.
        // Both FKs use Cascade so deleting either side removes the junction row.
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

        // --- Seed Data ---
        // Note: Admin account is seeded via appsettings.json on startup.
        modelBuilder.Entity<SystemAccount>().HasData(
            new SystemAccount
            {
                AccountID = 1,
                AccountName = "Test Staff",
                AccountEmail = "staff@FUNewsTradingSystem.org",
                AccountRole = 1,
                AccountPassword = "@@abc123@@_HASH_PLACEHOLDER"
            },
            new SystemAccount
            {
                AccountID = 2,
                AccountName = "Test Lecturer",
                AccountEmail = "lecturer@FUNewsTradingSystem.org",
                AccountRole = 2,
                AccountPassword = "@@abc123@@_HASH_PLACEHOLDER"
            }
        );

        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryID = 1, CategoryName = "Technology", CategoryDescription = "Technology sector", IsActive = true },
            new Category { CategoryID = 2, CategoryName = "Healthcare", CategoryDescription = "Healthcare sector", IsActive = true },
            new Category { CategoryID = 3, CategoryName = "Finance", CategoryDescription = "Financial sector", IsActive = true },
            new Category { CategoryID = 4, CategoryName = "Energy", CategoryDescription = "Energy sector", IsActive = true },
            new Category { CategoryID = 5, CategoryName = "Cryptocurrencies", CategoryDescription = "Digital assets", IsActive = true },
            new Category { CategoryID = 6, CategoryName = "Consumer Goods", CategoryDescription = "Goods bought and used by consumers", IsActive = true }
        );

        modelBuilder.Entity<Tag>().HasData(
            new Tag { TagID = 1, TagName = "AAPL", Note = "Apple Inc." },
            new Tag { TagID = 2, TagName = "NVDA", Note = "NVIDIA Corporation" },
            new Tag { TagID = 3, TagName = "MSFT", Note = "Microsoft Corporation" },
            new Tag { TagID = 4, TagName = "GOOGL", Note = "Alphabet Inc. (Google)" },
            new Tag { TagID = 5, TagName = "TSLA", Note = "Tesla, Inc." },
            new Tag { TagID = 6, TagName = "BTC", Note = "Bitcoin" },
            new Tag { TagID = 7, TagName = "ETH", Note = "Ethereum" },
            new Tag { TagID = 8, TagName = "AMZN", Note = "Amazon.com, Inc." }
        );

        // --- SavedReport ---
        // Unique per (AccountID, NewsArticleID) — no duplicate bookmarks.
        // Cascade delete: removing account or article cleans up its saved rows.
        modelBuilder.Entity<SavedReport>(entity =>
        {
            entity.ToTable("SavedReport");
            entity.HasIndex(e => new { e.AccountID, e.NewsArticleID }).IsUnique();
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(sr => sr.Account)
                  .WithMany()
                  .HasForeignKey(sr => sr.AccountID)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sr => sr.NewsArticle)
                  .WithMany()
                  .HasForeignKey(sr => sr.NewsArticleID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- TagCategoryMap ---
        // Maps which tags belong to which category (sector).
        // Unique per (TagID, CategoryID) — prevents duplicate pairings.
        modelBuilder.Entity<TagCategoryMap>(entity =>
        {
            entity.ToTable("TagCategoryMap");
            entity.HasIndex(e => new { e.TagID, e.CategoryID }).IsUnique();

            entity.HasOne(tcm => tcm.Tag)
                  .WithMany()
                  .HasForeignKey(tcm => tcm.TagID)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tcm => tcm.Category)
                  .WithMany()
                  .HasForeignKey(tcm => tcm.CategoryID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TagCategoryMap seed data: maps tickers to their market sector
        modelBuilder.Entity<TagCategoryMap>().HasData(
            // Technology sector (CategoryID=1)
            new TagCategoryMap { TagCategoryMapID = 1, TagID = 1, CategoryID = 1 }, // AAPL
            new TagCategoryMap { TagCategoryMapID = 2, TagID = 2, CategoryID = 1 }, // NVDA
            new TagCategoryMap { TagCategoryMapID = 3, TagID = 3, CategoryID = 1 }, // MSFT
            new TagCategoryMap { TagCategoryMapID = 4, TagID = 4, CategoryID = 1 }, // GOOGL
            new TagCategoryMap { TagCategoryMapID = 5, TagID = 5, CategoryID = 1 }, // TSLA
            new TagCategoryMap { TagCategoryMapID = 6, TagID = 8, CategoryID = 1 }, // AMZN
            // Cryptocurrencies sector (CategoryID=5)
            new TagCategoryMap { TagCategoryMapID = 7, TagID = 6, CategoryID = 5 }, // BTC
            new TagCategoryMap { TagCategoryMapID = 8, TagID = 7, CategoryID = 5 }  // ETH
        );

    }
}
