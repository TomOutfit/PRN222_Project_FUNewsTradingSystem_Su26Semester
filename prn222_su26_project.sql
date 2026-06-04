-- =====================================================================================
-- Database Script for FUNewsTradingSystem (FUNewsTradingSystem)
-- Role: P2 - Backend Developer
-- Description: Creates the database schema, tables, relationships, and seed data.
-- target RDBMS: SQL Server
-- =====================================================================================

USE master;
GO

IF DB_ID('FUNewsTradingSystem') IS NOT NULL
BEGIN
    ALTER DATABASE FUNewsTradingSystem SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE FUNewsTradingSystem;
END
GO

CREATE DATABASE FUNewsTradingSystem;
GO

USE FUNewsTradingSystem;
GO

-- =====================================================================================
-- 1. Create Tables
-- =====================================================================================

-- Table: SystemAccount
CREATE TABLE SystemAccount (
    AccountID INT IDENTITY(1,1) NOT NULL,
    AccountName NVARCHAR(100) NOT NULL,
    AccountEmail NVARCHAR(200) NOT NULL,
    AccountRole INT NOT NULL, -- 1=Staff, 2=Lecturer, 3=Admin
    AccountPassword NVARCHAR(500) NOT NULL,
    CONSTRAINT PK_SystemAccount PRIMARY KEY (AccountID),
    CONSTRAINT UQ_SystemAccount_AccountEmail UNIQUE (AccountEmail)
);
GO

-- Table: Category
CREATE TABLE Category (
    CategoryID INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(200) NOT NULL,
    CategoryDescription NVARCHAR(500) NULL,
    ParentCategoryID INT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Category_IsActive DEFAULT 1,
    CONSTRAINT PK_Category PRIMARY KEY (CategoryID),
    CONSTRAINT FK_Category_ParentCategory FOREIGN KEY (ParentCategoryID) 
        REFERENCES Category(CategoryID) 
        ON DELETE NO ACTION
);
GO

-- Table: Tag
CREATE TABLE Tag (
    TagID INT IDENTITY(1,1) NOT NULL,
    TagName NVARCHAR(50) NOT NULL,
    Note NVARCHAR(500) NULL,
    CONSTRAINT PK_Tag PRIMARY KEY (TagID),
    CONSTRAINT UQ_Tag_TagName UNIQUE (TagName)
);
GO

-- Table: NewsArticle
CREATE TABLE NewsArticle (
    NewsArticleID INT IDENTITY(1,1) NOT NULL,
    NewsTitle NVARCHAR(500) NOT NULL,
    Headline NVARCHAR(1000) NOT NULL,
    CreatedDate DATETIME NOT NULL,
    NewsContent NVARCHAR(MAX) NOT NULL,
    NewsSource NVARCHAR(500) NULL,
    CategoryID INT NOT NULL,
    NewsStatus BIT NOT NULL CONSTRAINT DF_NewsArticle_NewsStatus DEFAULT 1,
    CreatedByID INT NULL,
    UpdatedByID INT NULL,
    ModifiedDate DATETIME NULL,
    CONSTRAINT PK_NewsArticle PRIMARY KEY (NewsArticleID),
    CONSTRAINT FK_NewsArticle_Category FOREIGN KEY (CategoryID) 
        REFERENCES Category(CategoryID) 
        ON DELETE NO ACTION, -- Block category deletion if referenced
    CONSTRAINT FK_NewsArticle_CreatedBy FOREIGN KEY (CreatedByID) 
        REFERENCES SystemAccount(AccountID) 
        ON DELETE SET NULL,  -- Keep article but set creator to NULL
    CONSTRAINT FK_NewsArticle_UpdatedBy FOREIGN KEY (UpdatedByID) 
        REFERENCES SystemAccount(AccountID) 
        ON DELETE NO ACTION
);
GO

-- Table: NewsTag (Junction Table)
CREATE TABLE NewsTag (
    NewsArticleID INT NOT NULL,
    TagID INT NOT NULL,
    CONSTRAINT PK_NewsTag PRIMARY KEY (NewsArticleID, TagID),
    CONSTRAINT FK_NewsTag_NewsArticle FOREIGN KEY (NewsArticleID) 
        REFERENCES NewsArticle(NewsArticleID) 
        ON DELETE CASCADE,
    CONSTRAINT FK_NewsTag_Tag FOREIGN KEY (TagID) 
        REFERENCES Tag(TagID) 
        ON DELETE NO ACTION -- Block tag deletion if referenced
);
GO

-- =====================================================================================
-- 2. Initial Seed Data
-- =====================================================================================

-- SystemAccount 
-- Note: In the actual application, the password will be hashed using ASP.NET Core IPasswordHasher
INSERT INTO SystemAccount (AccountName, AccountEmail, AccountRole, AccountPassword)
VALUES ('System Admin', 'admin@FUNewsTradingSystem.org', 3, '@@abc123@@_HASH_PLACEHOLDER');
GO

-- Seed Categories
INSERT INTO Category (CategoryName, CategoryDescription, IsActive) VALUES 
('Technology', 'Technology sector including software, hardware, and IT services', 1),
('Healthcare', 'Healthcare sector including pharmaceuticals and medical devices', 1),
('Finance', 'Financial sector including banking and investment', 1),
('Energy', 'Energy sector including oil, gas, and renewable energy', 1),
('Cryptocurrencies', 'Digital assets and blockchain technology', 1),
('Consumer Goods', 'Goods bought and used by consumers', 1);
GO

-- Seed Tags
INSERT INTO Tag (TagName, Note) VALUES 
('AAPL', 'Apple Inc.'),
('NVDA', 'NVIDIA Corporation'),
('MSFT', 'Microsoft Corporation'),
('GOOGL', 'Alphabet Inc. (Google)'),
('TSLA', 'Tesla, Inc.'),
('BTC', 'Bitcoin'),
('ETH', 'Ethereum'),
('AMZN', 'Amazon.com, Inc.');
GO
