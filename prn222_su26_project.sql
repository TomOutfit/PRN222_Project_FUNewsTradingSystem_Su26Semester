-- =====================================================================================
-- Database Script for FUNewsTradingSystem (FUNewsTradingSystem)
-- Role: P2 - Backend Developer
-- Description: Creates the database schema, tables, relationships, and extended seed data.
-- Target RDBMS: SQL Server
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
-- 1. Drop existing tables (in correct order due to FK constraints)
-- =====================================================================================
IF OBJECT_ID('SavedReport', 'U') IS NOT NULL DROP TABLE SavedReport;
IF OBJECT_ID('TagCategoryMap', 'U') IS NOT NULL DROP TABLE TagCategoryMap;
IF OBJECT_ID('NewsTag', 'U') IS NOT NULL DROP TABLE NewsTag;
IF OBJECT_ID('NewsArticle', 'U') IS NOT NULL DROP TABLE NewsArticle;
IF OBJECT_ID('Tag', 'U') IS NOT NULL DROP TABLE Tag;
IF OBJECT_ID('Category', 'U') IS NOT NULL DROP TABLE Category;
IF OBJECT_ID('SystemAccount', 'U') IS NOT NULL DROP TABLE SystemAccount;
GO

-- =====================================================================================
-- 2. Create Tables
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
    ConfidenceScore INT NULL,
    CONSTRAINT PK_NewsArticle PRIMARY KEY (NewsArticleID),
    CONSTRAINT FK_NewsArticle_Category FOREIGN KEY (CategoryID)
        REFERENCES Category(CategoryID)
        ON DELETE NO ACTION,
    CONSTRAINT FK_NewsArticle_CreatedBy FOREIGN KEY (CreatedByID)
        REFERENCES SystemAccount(AccountID)
        ON DELETE SET NULL,
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
        ON DELETE NO ACTION
);
GO

-- Table: SavedReport (User bookmarks)
CREATE TABLE SavedReport (
    SavedReportID INT IDENTITY(1,1) NOT NULL,
    AccountID INT NOT NULL,
    NewsArticleID INT NOT NULL,
    SavedDate DATETIME NOT NULL CONSTRAINT DF_SavedReport_SavedDate DEFAULT GETUTCDATE(),
    Notes NVARCHAR(1000) NULL,
    CONSTRAINT PK_SavedReport PRIMARY KEY (SavedReportID),
    CONSTRAINT UQ_SavedReport_AccountArticle UNIQUE (AccountID, NewsArticleID),
    CONSTRAINT FK_SavedReport_SystemAccount FOREIGN KEY (AccountID)
        REFERENCES SystemAccount(AccountID)
        ON DELETE CASCADE,
    CONSTRAINT FK_SavedReport_NewsArticle FOREIGN KEY (NewsArticleID)
        REFERENCES NewsArticle(NewsArticleID)
        ON DELETE CASCADE
);
GO

-- Table: TagCategoryMap (Tag → Sector mapping)
CREATE TABLE TagCategoryMap (
    TagCategoryMapID INT IDENTITY(1,1) NOT NULL,
    TagID INT NOT NULL,
    CategoryID INT NOT NULL,
    CONSTRAINT PK_TagCategoryMap PRIMARY KEY (TagCategoryMapID),
    CONSTRAINT UQ_TagCategoryMap_TagCategory UNIQUE (TagID, CategoryID),
    CONSTRAINT FK_TagCategoryMap_Tag FOREIGN KEY (TagID)
        REFERENCES Tag(TagID)
        ON DELETE CASCADE,
    CONSTRAINT FK_TagCategoryMap_Category FOREIGN KEY (CategoryID)
        REFERENCES Category(CategoryID)
        ON DELETE CASCADE
);
GO

-- =====================================================================================
-- 3. Seed Data — Base Accounts
-- =====================================================================================
INSERT INTO SystemAccount (AccountName, AccountEmail, AccountRole, AccountPassword)
VALUES
    ('John Operator', 'john.staff@FUNewsTradingSystem.org', 1, '@@abc123@@_HASH_PLACEHOLDER'),
    ('Sarah Analyst', 'sarah.staff@FUNewsTradingSystem.org', 1, '@@abc123@@_HASH_PLACEHOLDER'),
    ('Mike Trader', 'mike.staff@FUNewsTradingSystem.org', 1, '@@abc123@@_HASH_PLACEHOLDER'),
    ('Dr. Alan Smith', 'alan.lecturer@FUNewsTradingSystem.org', 2, '@@abc123@@_HASH_PLACEHOLDER'),
    ('Prof. Maria Garcia', 'maria.lecturer@FUNewsTradingSystem.org', 2, '@@abc123@@_HASH_PLACEHOLDER');

-- EF Core test accounts (mirrored for prod/demo testing)
INSERT INTO SystemAccount (AccountName, AccountEmail, AccountRole, AccountPassword)
VALUES
    ('Test Staff',    'staff@FUNewsTradingSystem.org',    1, '@@abc123@@_HASH_PLACEHOLDER'),
    ('Test Lecturer', 'lecturer@FUNewsTradingSystem.org', 2, '@@abc123@@_HASH_PLACEHOLDER');
GO

-- =====================================================================================
-- 4. Seed Data — Categories
-- =====================================================================================
-- Top-level categories (IDs 1-6 via IDENTITY)
INSERT INTO Category (CategoryName, CategoryDescription, IsActive) VALUES
('Technology', 'Technology sector including software, hardware, and IT services', 1),
('Healthcare', 'Healthcare sector including pharmaceuticals and medical devices', 1),
('Finance', 'Financial sector including banking and investment', 1),
('Energy', 'Energy sector including oil, gas, and renewable energy', 1),
('Cryptocurrencies', 'Digital assets and blockchain technology', 1),
('Consumer Goods', 'Goods bought and used by consumers', 1);

-- Sub-categories (IDs 7-12)
INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive) VALUES
('Software & AI', 'Companies developing software solutions and artificial intelligence', 1, 1),
('Semiconductors', 'Hardware manufacturers of computer chips and GPUs', 1, 1),
('Biotechnology', 'Companies applying biological processes to manufacturing', 2, 1),
('Commercial Banking', 'Traditional consumer and business banking institutions', 3, 1),
('Green Energy', 'Renewable resources such as solar and wind', 4, 1),
('E-commerce', 'Online retail and distribution', 6, 1);
GO

-- =====================================================================================
-- 5. Seed Data — Tags (Tickers)
-- =====================================================================================
INSERT INTO Tag (TagName, Note) VALUES
('AAPL', 'Apple Inc.'),
('NVDA', 'NVIDIA Corporation'),
('MSFT', 'Microsoft Corp.'),
('GOOGL', 'Alphabet Inc.'),
('TSLA', 'Tesla, Inc.'),
('BTC', 'Bitcoin'),
('ETH', 'Ethereum'),
('AMZN', 'Amazon.com, Inc.'),
('META', 'Meta Platforms'),
('JPM', 'JPMorgan Chase & Co.'),
('PFE', 'Pfizer Inc.'),
('XOM', 'Exxon Mobil Corp.');
GO

-- =====================================================================================
-- 6. Seed Data — News Articles
-- =====================================================================================
INSERT INTO NewsArticle (NewsTitle, Headline, CreatedDate, NewsContent, NewsSource, CategoryID, NewsStatus, CreatedByID, UpdatedByID, ModifiedDate) VALUES
(
    '[BUY] NVDA Automated Analysis',
    'NVIDIA maintains market dominance with next-generation AI chip announcements.',
    DATEADD(day, -2, GETUTCDATE()),
    '(1) Sentiment: Highly positive across all financial media due to unexpected revenue beats. (2) Fundamental: Unprecedented pricing power and profit margins with their latest Blackwell architecture. (3) Risks: Geopolitical tensions affecting Taiwan supply chains.',
    'NewsAPI.org + gpt-4o',
    8, 1, 1, 1, DATEADD(day, -2, GETUTCDATE())
),
(
    '[SELL] TSLA Automated Analysis',
    'Increasing competition in EV space and shrinking margins trigger concerns.',
    DATEADD(day, -5, GETUTCDATE()),
    '(1) Sentiment: Negative to neutral, reflecting investor anxiety over recent price cuts. (2) Fundamental: Margins are compressing as the company prioritizes volume over profitability. (3) Risks: Rising interest rates making auto financing difficult and delays in autonomous driving tech.',
    'NewsAPI.org + gpt-4o',
    6, 1, 2, 2, DATEADD(day, -5, GETUTCDATE())
),
(
    '[HOLD] AAPL Automated Analysis',
    'Steady services growth offsets sluggish hardware cycle amidst regulatory scrutiny.',
    DATEADD(day, -10, GETUTCDATE()),
    '(1) Sentiment: Neutral. The market is awaiting the next major iPhone upgrade cycle. (2) Fundamental: Incredible cash generation and share buybacks provide a high floor, but growth is currently stagnant. (3) Risks: EU regulatory fines and anti-trust lawsuits impacting App Store revenue.',
    'NewsAPI.org + gpt-4o',
    1, 1, 1, 1, DATEADD(day, -10, GETUTCDATE())
),
(
    '[BUY] BTC Automated Analysis',
    'Institutional adoption accelerates following successful ETF launches.',
    DATEADD(day, -1, GETUTCDATE()),
    '(1) Sentiment: Overwhelmingly positive as major financial institutions allocate capital. (2) Fundamental: Supply shock post-halving combined with sustained ETF inflows creates strong upward pressure. (3) Risks: Sudden macroeconomic shifts or harsh regulatory crackdowns in major jurisdictions.',
    'NewsAPI.org + gpt-4o',
    5, 1, 3, 3, DATEADD(day, -1, GETUTCDATE())
),
(
    '[SELL] PFE Automated Analysis',
    'Post-pandemic revenue slump continues to weigh heavily on valuation.',
    DATEADD(day, -15, GETUTCDATE()),
    '(1) Sentiment: Negative. Investors are rotating out of the stock due to a lack of immediate catalysts. (2) Fundamental: Steep drop-off in vaccine and antiviral revenues with a thin near-term pipeline. (3) Risks: Failure in upcoming phase 3 trials could further depress the stock.',
    'NewsAPI.org + gpt-4o',
    9, 0, 2, 2, DATEADD(day, -1, GETUTCDATE())
),
(
    '[HOLD] AAPL Automated Analysis',
    'Steady services growth offsets sluggish hardware cycle amidst regulatory scrutiny.',
    DATEADD(day, -10, GETUTCDATE()),
    '(1) Sentiment: Neutral. The market is awaiting the next major iPhone upgrade cycle. (2) Fundamental: Incredible cash generation and share buybacks provide a high floor, but growth is currently stagnant. (3) Risks: EU regulatory fines and anti-trust lawsuits impacting App Store revenue.',
    'NewsAPI.org + gpt-4o',
    1, 1, 1, 1, DATEADD(day, -10, GETUTCDATE())
),
(
    '[BUY] MSFT Automated Analysis',
    'Microsoft maintains market dominance with next-generation AI chip announcements.',
    DATEADD(day, -2, GETUTCDATE()),
    '(1) Sentiment: Highly positive across all financial media due to unexpected revenue beats. (2) Fundamental: Unprecedented pricing power and profit margins with their latest Blackwell architecture. (3) Risks: Geopolitical tensions affecting Taiwan supply chains.',
    'NewsAPI.org + gpt-4o',
    8, 1, 1, 1, DATEADD(day, -2, GETUTCDATE())
);
GO

-- =====================================================================================
-- 7. Seed Data — NewsTags
-- =====================================================================================
INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (1, 2);   -- NVDA
INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (2, 5);   -- TSLA
INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (3, 1);   -- AAPL
INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (4, 6);   -- BTC
INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (5, 11); -- PFE
GO

-- =====================================================================================
-- 8. Seed Data — TagCategoryMap (base 12 tags → correct sector)
-- Base CategoryIDs (IDENTITY order from Section 4):
--   1=Technology, 2=Healthcare, 3=Finance, 4=Energy, 5=Cryptocurrencies, 6=Consumer Goods
--   7=Software & AI, 8=Semiconductors, 9=Biotechnology, 10=Commercial Banking
--   11=Green Energy, 12=E-commerce
-- Base TagIDs (IDENTITY order from Section 5):
--   1=AAPL, 2=NVDA, 3=MSFT, 4=GOOGL, 5=TSLA, 6=BTC, 7=ETH, 8=AMZN, 9=META, 10=JPM, 11=PFE, 12=XOM
-- =====================================================================================
INSERT INTO TagCategoryMap (TagID, CategoryID) VALUES
-- Technology sector (CategoryID=1)
(1, 1),   -- AAPL → Technology
(5, 1),   -- TSLA → Technology
(8, 1),   -- AMZN → Technology
-- Semiconductors (CategoryID=8)
(2, 8),   -- NVDA → Semiconductors
-- Software & AI (CategoryID=7)
(3, 7),   -- MSFT → Software & AI
(4, 7),   -- GOOGL → Software & AI
(9, 7),   -- META → Software & AI
-- Cryptocurrencies (CategoryID=5)
(6, 5),   -- BTC → Cryptocurrencies
(7, 5),   -- ETH → Cryptocurrencies
-- Finance (CategoryID=3)
(10, 3),  -- JPM → Finance
-- Healthcare (CategoryID=2)
(11, 2),  -- PFE → Healthcare
-- Energy (CategoryID=4)
(12, 4);  -- XOM → Energy
GO

PRINT 'Base seed data deployed successfully!';
GO

-- =====================================================================================
-- BULK TEST DATA EXPANSION
-- Purpose: Test pagination, admin statistics, staff report history, and filtering
-- =====================================================================================

PRINT '=== BULK SEED: Starting...';
GO

-- =====================================================================================
-- SECTION A — BULK ACCOUNTS (80 Staff + 40 Lecturer = 120)
-- =====================================================================================
;WITH Numbers AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1 FROM Numbers WHERE n < 120
),
FirstNames(id, val) AS (
    SELECT 1, N'Nguyen' UNION ALL SELECT 2, N'Tran' UNION ALL SELECT 3, N'Le' UNION ALL SELECT 4, N'Pham' UNION ALL
    SELECT 5, N'Hoang' UNION ALL SELECT 6, N'Huynh' UNION ALL SELECT 7, N'Phan' UNION ALL SELECT 8, N'Vu' UNION ALL
    SELECT 9, N'Vo' UNION ALL SELECT 10, N'Dang' UNION ALL SELECT 11, N'Bui' UNION ALL SELECT 12, N'Do' UNION ALL
    SELECT 13, N'Ho' UNION ALL SELECT 14, N'Ngo' UNION ALL SELECT 15, N'Duong' UNION ALL SELECT 16, N'Ly'
),
MiddleNames(id, val) AS (
    SELECT 1, N'Van' UNION ALL SELECT 2, N'Thi' UNION ALL SELECT 3, N'Minh' UNION ALL SELECT 4, N'Anh' UNION ALL
    SELECT 5, N'Duc' UNION ALL SELECT 6, N'Huy' UNION ALL SELECT 7, N'Hai' UNION ALL SELECT 8, N'Quoc' UNION ALL
    SELECT 9, N'Hoai' UNION ALL SELECT 10, N'Thanh'
),
LastNames(id, val) AS (
    SELECT 1, N'Anh' UNION ALL SELECT 2, N'Bao' UNION ALL SELECT 3, N'Cuong' UNION ALL SELECT 4, N'Dung' UNION ALL
    SELECT 5, N'Hung' UNION ALL SELECT 6, N'Khoa' UNION ALL SELECT 7, N'Linh' UNION ALL SELECT 8, N'Nam' UNION ALL
    SELECT 9, N'Phong' UNION ALL SELECT 10, N'Son' UNION ALL SELECT 11, N'Trang' UNION ALL SELECT 12, N'Tuan' UNION ALL
    SELECT 13, N'Viet' UNION ALL SELECT 14, N'Vy' UNION ALL SELECT 15, N'Yen' UNION ALL SELECT 16, N'Khanh'
)
INSERT INTO SystemAccount (AccountName, AccountEmail, AccountRole, AccountPassword)
SELECT
    f.val + ' ' + m.val + ' ' + l.val,
    CASE WHEN n <= 80
         THEN LOWER(l.val + '.' + f.val + CAST(n AS VARCHAR(10)) + '.staff@FUNewsTradingSystem.org')
         ELSE LOWER(l.val + '.' + f.val + CAST(n - 80 AS VARCHAR(10)) + '.lecturer@FUNewsTradingSystem.org')
    END,
    CASE WHEN n <= 80 THEN 1 ELSE 2 END,
    '@@abc123@@_HASH_PLACEHOLDER'
FROM Numbers num
JOIN FirstNames f ON f.id = ((num.n - 1) % 16) + 1
JOIN MiddleNames m ON m.id = (((num.n - 1) / 12) % 10) + 1
JOIN LastNames l ON l.id = (((num.n - 1) / 3) % 16) + 1
OPTION (MAXRECURSION 200);
GO

PRINT 'Section A done: accounts added.';
GO

-- =====================================================================================
-- SECTION B — BULK CATEGORIES
-- =====================================================================================

-- 6 new top-level sectors
INSERT INTO Category (CategoryName, CategoryDescription, IsActive) VALUES
('Industrials', 'Manufacturing, aerospace, and industrial machinery', 1),
('Telecommunications', 'Wireless carriers, media and streaming infrastructure', 1),
('Real Estate', 'REITs and property investment vehicles', 1),
('Utilities', 'Electric, water, and gas utility providers', 1),
('Materials', 'Mining, metals, and raw materials producers', 1),
('Deprecated Sector (Test)', 'Retired sector kept for IsActive=0 dropdown exclusion test', 0);
GO

-- Sub-categories under Technology (CategoryID=1)
INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT v.Name, v.Descr, c.CategoryID, 1
FROM Category c
CROSS APPLY (VALUES
    ('Cloud Computing', 'Cloud infrastructure and SaaS platforms'),
    ('Cybersecurity', 'Network and endpoint security vendors'),
    ('Social Media', 'Consumer social networking platforms')
) AS v(Name, Descr)
WHERE c.CategoryName = 'Technology';
GO

-- Sub-categories under Healthcare (CategoryID=2)
INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT v.Name, v.Descr, c.CategoryID, 1
FROM Category c
CROSS APPLY (VALUES
    ('Pharmaceuticals', 'Drug development and manufacturing'),
    ('Medical Devices', 'Diagnostic and surgical device makers'),
    ('Digital Health', 'Telehealth and health-tech platforms')
) AS v(Name, Descr)
WHERE c.CategoryName = 'Healthcare';
GO

-- Sub-categories under Finance (CategoryID=3)
INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT v.Name, v.Descr, c.CategoryID, 1
FROM Category c
CROSS APPLY (VALUES
    ('Investment Banking', 'M&A advisory and capital markets'),
    ('Insurance', 'Life, property, and casualty insurers'),
    ('Fintech', 'Digital payments and lending platforms')
) AS v(Name, Descr)
WHERE c.CategoryName = 'Finance';
GO

-- Sub-categories under Energy (CategoryID=4)
INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT v.Name, v.Descr, c.CategoryID, 1
FROM Category c
CROSS APPLY (VALUES
    ('Oil & Gas Exploration', 'Upstream exploration and production'),
    ('Nuclear Energy', 'Nuclear power generation and fuel supply')
) AS v(Name, Descr)
WHERE c.CategoryName = 'Energy';
GO

-- Sub-categories under Cryptocurrencies (CategoryID=5)
INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT v.Name, v.Descr, c.CategoryID, 1
FROM Category c
CROSS APPLY (VALUES
    ('DeFi', 'Decentralized finance protocols and tokens'),
    ('NFT & Gaming Tokens', 'Gaming and non-fungible token ecosystems'),
    ('Stablecoins', 'Fiat-pegged digital currencies')
) AS v(Name, Descr)
WHERE c.CategoryName = 'Cryptocurrencies';
GO

-- Sub-categories under Consumer Goods (CategoryID=6)
INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT v.Name, v.Descr, c.CategoryID, 1
FROM Category c
CROSS APPLY (VALUES
    ('Apparel & Footwear', 'Clothing and footwear brands'),
    ('Food & Beverage', 'Packaged food and beverage producers')
) AS v(Name, Descr)
WHERE c.CategoryName = 'Consumer Goods';
GO

-- Sub-categories under Industrials
INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT v.Name, v.Descr, c.CategoryID, 1
FROM Category c
CROSS APPLY (VALUES
    ('Aerospace & Defense', 'Aircraft and defense contractors'),
    ('Industrial Machinery', 'Heavy equipment and automation')
) AS v(Name, Descr)
WHERE c.CategoryName = 'Industrials';
GO

INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT v.Name, v.Descr, c.CategoryID, 1
FROM Category c
CROSS APPLY (VALUES
    ('Wireless Carriers', 'Mobile network operators'),
    ('Media & Streaming', 'Content production and distribution')
) AS v(Name, Descr)
WHERE c.CategoryName = 'Telecommunications';
GO

INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT 'REITs', 'Real estate investment trusts', c.CategoryID, 1
FROM Category c WHERE c.CategoryName = 'Real Estate';
GO

INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT 'Electric Utilities', 'Regulated electric power providers', c.CategoryID, 1
FROM Category c WHERE c.CategoryName = 'Utilities';
GO

INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT 'Mining & Metals', 'Metal ore extraction and processing', c.CategoryID, 1
FROM Category c WHERE c.CategoryName = 'Materials';
GO

PRINT 'Section B done: categories added.';
GO

-- =====================================================================================
-- SECTION C — BULK TAGS (~90 new tickers, total ~102)
-- Bulk Tag IDs (IDENTITY order, starting at 13):
--   13=NFLX, 14=DIS, 15=KO, 16=PEP, 17=MCD, 18=SBUX, 19=NKE, 20=WMT, 21=TGT, 22=COST,
--   23=HD, 24=LOW, 25=BA, 26=GE, 27=CAT, 28=MMM, 29=IBM, 30=INTC, 31=AMD, 32=QCOM,
--   33=ORCL, 34=CRM, 35=ADBE, 36=PYPL, 37=V, 38=MA, 39=BAC, 40=WFC, 41=GS, 42=MS,
--   43=C, 44=UNH, 45=JNJ, 46=MRNA, 47=LLY, 48=ABBV, 49=CVX, 50=COP, 51=SLB,
--   52=NEE, 53=DUK, 54=T, 55=VZ, 56=TMUS, 57=CMCSA, 58=SONY, 59=SNAP, 60=PINS,
--   61=UBER, 62=LYFT, 63=ABNB, 64=DASH, 65=SHOP, 66=SQ, 67=COIN, 68=RIVN, 69=LCID,
--   70=F, 71=GM, 72=TM, 73=HMC, 74=SPOT, 75=RBLX, 76=U, 77=PLTR, 78=SNOW, 79=DDOG,
--   80=NET, 81=ZM, 82=DOCU, 83=TEAM, 84=NOW, 85=WDAY, 86=PANW, 87=CRWD, 88=OKTA,
--   89=MDB, 90=TWLO, 91=ETSY, 92=EBAY, 93=BABA, 94=JD, 95=PDD, 96=SE, 97=MELI,
--   98=SOL, 99=ADA, 100=DOGE, 101=XRP, 102=BNB, 103=LTC, 104=DOT, 105=LINK
-- =====================================================================================
INSERT INTO Tag (TagName, Note)
SELECT v.TagName, v.Note
FROM (VALUES
    ('NFLX','Netflix, Inc.'), ('DIS','The Walt Disney Company'), ('KO','The Coca-Cola Company'),
    ('PEP','PepsiCo, Inc.'), ('MCD','McDonald''s Corporation'), ('SBUX','Starbucks Corporation'),
    ('NKE','Nike, Inc.'), ('WMT','Walmart Inc.'), ('TGT','Target Corporation'),
    ('COST','Costco Wholesale Corporation'), ('HD','The Home Depot, Inc.'), ('LOW','Lowe''s Companies, Inc.'),
    ('BA','The Boeing Company'), ('GE','General Electric Company'), ('CAT','Caterpillar Inc.'),
    ('MMM','3M Company'), ('IBM','International Business Machines Corporation'), ('INTC','Intel Corporation'),
    ('AMD','Advanced Micro Devices, Inc.'), ('QCOM','Qualcomm Incorporated'), ('ORCL','Oracle Corporation'),
    ('CRM','Salesforce, Inc.'), ('ADBE','Adobe Inc.'), ('PYPL','PayPal Holdings, Inc.'),
    ('V','Visa Inc.'), ('MA','Mastercard Incorporated'), ('BAC','Bank of America Corporation'),
    ('WFC','Wells Fargo & Company'), ('GS','The Goldman Sachs Group, Inc.'), ('MS','Morgan Stanley'),
    ('C','Citigroup Inc.'), ('UNH','UnitedHealth Group Incorporated'), ('JNJ','Johnson & Johnson'),
    ('MRNA','Moderna, Inc.'), ('LLY','Eli Lilly and Company'), ('ABBV','AbbVie Inc.'),
    ('CVX','Chevron Corporation'), ('COP','ConocoPhillips'), ('SLB','Schlumberger Limited'),
    ('NEE','NextEra Energy, Inc.'), ('DUK','Duke Energy Corporation'), ('T','AT&T Inc.'),
    ('VZ','Verizon Communications Inc.'), ('TMUS','T-Mobile US, Inc.'), ('CMCSA','Comcast Corporation'),
    ('SONY','Sony Group Corporation'), ('SNAP','Snap Inc.'), ('PINS','Pinterest, Inc.'),
    ('UBER','Uber Technologies, Inc.'), ('LYFT','Lyft, Inc.'), ('ABNB','Airbnb, Inc.'),
    ('DASH','DoorDash, Inc.'), ('SHOP','Shopify Inc.'), ('SQ','Block, Inc.'),
    ('COIN','Coinbase Global, Inc.'), ('RIVN','Rivian Automotive, Inc.'), ('LCID','Lucid Group, Inc.'),
    ('F','Ford Motor Company'), ('GM','General Motors Company'), ('TM','Toyota Motor Corporation'),
    ('HMC','Honda Motor Co., Ltd.'), ('SPOT','Spotify Technology S.A.'), ('RBLX','Roblox Corporation'),
    ('U','Unity Software Inc.'), ('PLTR','Palantir Technologies Inc.'), ('SNOW','Snowflake Inc.'),
    ('DDOG','Datadog, Inc.'), ('NET','Cloudflare, Inc.'), ('ZM','Zoom Video Communications, Inc.'),
    ('DOCU','DocuSign, Inc.'), ('TEAM','Atlassian Corporation'), ('NOW','ServiceNow, Inc.'),
    ('WDAY','Workday, Inc.'), ('PANW','Palo Alto Networks, Inc.'), ('CRWD','CrowdStrike Holdings, Inc.'),
    ('OKTA','Okta, Inc.'), ('MDB','MongoDB, Inc.'), ('TWLO','Twilio Inc.'),
    ('ETSY','Etsy, Inc.'), ('EBAY','eBay Inc.'), ('BABA','Alibaba Group Holding Limited'),
    ('JD','JD.com, Inc.'), ('PDD','PDD Holdings Inc.'), ('SE','Sea Limited'),
    ('MELI','MercadoLibre, Inc.'), ('SOL','Solana'), ('ADA','Cardano'),
    ('DOGE','Dogecoin'), ('XRP','Ripple'), ('BNB','Binance Coin'),
    ('LTC','Litecoin'), ('DOT','Polkadot'), ('LINK','Chainlink')
) AS v(TagName, Note)
WHERE NOT EXISTS (SELECT 1 FROM Tag t WHERE t.TagName = v.TagName);
GO

PRINT 'Section C done: tags added.';
GO

-- =====================================================================================
-- SECTION C+ — TAG → CATEGORY MAPPINGS (bulk ~93 tags mapped to sectors)
-- Runs AFTER Section C so all TagName values are present.
-- Uses subquery lookup so IDs are resolved correctly regardless of IDENTITY order.
-- =====================================================================================
-- Technology / Semiconductors / Software & AI
-- CategoryIDs: 1=Technology, 7=Software & AI, 8=Semiconductors
-- TagIDs resolved via subquery: AAPL=1, NVDA=2, MSFT=3, GOOGL=4, TSLA=5, AMZN=8
--   NFLX=13, DIS=14, KO=15, PEP=16, MCD=17, SBUX=18, NKE=19, WMT=20, TGT=21, COST=22,
--   HD=23, LOW=24, BA=25, GE=26, CAT=27, MMM=28, IBM=29, INTC=30, AMD=31, QCOM=32,
--   ORCL=33, CRM=34, ADBE=35, SONY=58, SNAP=59, PINS=60, U=76, PLTR=77, SNOW=78,
--   DDOG=79, NET=80, ZM=81, DOCU=82, TEAM=83, NOW=84, WDAY=85, PANW=86, CRWD=87,
--   OKTA=88, MDB=89, TWLO=90, SPOT=74, RBLX=75
-- =====================================================================================
INSERT INTO TagCategoryMap (TagID, CategoryID)
SELECT (SELECT TagID FROM Tag WHERE TagName = v.TagName), v.CatID
FROM (VALUES
    -- Technology
    ('AAPL', 1), ('TSLA', 1), ('AMZN', 1), ('SONY', 1),
    -- Semiconductors
    ('NVDA', 8), ('INTC', 8), ('AMD', 8), ('QCOM', 8),
    -- Software & AI
    ('MSFT', 7), ('GOOGL', 7), ('META', 7), ('IBM', 7),
    ('ORCL', 7), ('CRM', 7), ('ADBE', 7), ('SNAP', 7),
    ('PINS', 7), ('SPOT', 7), ('RBLX', 7), ('U', 7),
    ('PLTR', 7), ('SNOW', 7), ('DDOG', 7), ('NET', 7),
    ('ZM', 7), ('DOCU', 7), ('TEAM', 7), ('NOW', 7),
    ('WDAY', 7), ('PANW', 7), ('CRWD', 7), ('OKTA', 7),
    ('MDB', 7), ('TWLO', 7)
) AS v(TagName, CatID)
WHERE NOT EXISTS (
    SELECT 1 FROM TagCategoryMap tcm
    WHERE tcm.TagID = (SELECT TagID FROM Tag WHERE TagName = v.TagName)
      AND tcm.CategoryID = v.CatID
);
GO

-- Healthcare / Biotechnology
-- CategoryIDs: 2=Healthcare, 9=Biotechnology
-- PFE=11, UNH=44, JNJ=45, MRNA=46, LLY=47, ABBV=48
INSERT INTO TagCategoryMap (TagID, CategoryID)
SELECT (SELECT TagID FROM Tag WHERE TagName = v.TagName), v.CatID
FROM (VALUES
    ('PFE', 2), ('UNH', 2), ('JNJ', 2), ('LLY', 2),
    ('MRNA', 9), ('ABBV', 9)
) AS v(TagName, CatID)
WHERE NOT EXISTS (
    SELECT 1 FROM TagCategoryMap tcm
    WHERE tcm.TagID = (SELECT TagID FROM Tag WHERE TagName = v.TagName)
      AND tcm.CategoryID = v.CatID
);
GO

-- Finance / Commercial Banking
-- CategoryIDs: 3=Finance, 10=Commercial Banking
-- JPM=10, PYPL=36, V=37, MA=38, GS=41, MS=42, BAC=39, WFC=40, C=43, SQ=66
INSERT INTO TagCategoryMap (TagID, CategoryID)
SELECT (SELECT TagID FROM Tag WHERE TagName = v.TagName), v.CatID
FROM (VALUES
    ('JPM', 10), ('PYPL', 3), ('V', 3), ('MA', 3),
    ('BAC', 10), ('WFC', 10), ('GS', 3), ('MS', 3),
    ('C', 10), ('SQ', 3)
) AS v(TagName, CatID)
WHERE NOT EXISTS (
    SELECT 1 FROM TagCategoryMap tcm
    WHERE tcm.TagID = (SELECT TagID FROM Tag WHERE TagName = v.TagName)
      AND tcm.CategoryID = v.CatID
);
GO

-- Energy / Green Energy
-- CategoryIDs: 4=Energy, 11=Green Energy
-- XOM=12, CVX=49, COP=50, SLB=51, NEE=52, DUK=53
INSERT INTO TagCategoryMap (TagID, CategoryID)
SELECT (SELECT TagID FROM Tag WHERE TagName = v.TagName), v.CatID
FROM (VALUES
    ('XOM', 4), ('CVX', 4), ('COP', 4), ('SLB', 4),
    ('NEE', 11), ('DUK', 4)
) AS v(TagName, CatID)
WHERE NOT EXISTS (
    SELECT 1 FROM TagCategoryMap tcm
    WHERE tcm.TagID = (SELECT TagID FROM Tag WHERE TagName = v.TagName)
      AND tcm.CategoryID = v.CatID
);
GO

-- Consumer Goods / E-commerce
-- CategoryIDs: 6=Consumer Goods, 12=E-commerce
-- NFLX=13, DIS=14, KO=15, PEP=16, MCD=17, SBUX=18, NKE=19, WMT=20, TGT=21, COST=22,
-- HD=23, LOW=24, UBER=61, LYFT=62, ABNB=63, DASH=64, SHOP=65,
-- ETSY=91, EBAY=92, BABA=93, JD=94, PDD=95, SE=96, MELI=97
INSERT INTO TagCategoryMap (TagID, CategoryID)
SELECT (SELECT TagID FROM Tag WHERE TagName = v.TagName), v.CatID
FROM (VALUES
    ('DIS', 6), ('KO', 6), ('PEP', 6), ('MCD', 6),
    ('SBUX', 6), ('NKE', 6), ('WMT', 6), ('TGT', 6),
    ('COST', 6), ('HD', 6), ('LOW', 6),
    ('UBER', 6), ('LYFT', 6), ('ABNB', 6), ('DASH', 6),
    ('SHOP', 12), ('ETSY', 12), ('EBAY', 12),
    ('BABA', 12), ('JD', 12), ('PDD', 12), ('SE', 12), ('MELI', 12)
) AS v(TagName, CatID)
WHERE NOT EXISTS (
    SELECT 1 FROM TagCategoryMap tcm
    WHERE tcm.TagID = (SELECT TagID FROM Tag WHERE TagName = v.TagName)
      AND tcm.CategoryID = v.CatID
);
GO

-- Cryptocurrencies
-- CategoryID: 5=Cryptocurrencies
-- BTC=6, ETH=7, COIN=67, SOL=98, ADA=99, DOGE=100, XRP=101, BNB=102, LTC=103, DOT=104, LINK=105
INSERT INTO TagCategoryMap (TagID, CategoryID)
SELECT (SELECT TagID FROM Tag WHERE TagName = v.TagName), v.CatID
FROM (VALUES
    ('BTC', 5), ('ETH', 5), ('COIN', 5),
    ('SOL', 5), ('ADA', 5), ('DOGE', 5),
    ('XRP', 5), ('BNB', 5), ('LTC', 5), ('DOT', 5), ('LINK', 5)
) AS v(TagName, CatID)
WHERE NOT EXISTS (
    SELECT 1 FROM TagCategoryMap tcm
    WHERE tcm.TagID = (SELECT TagID FROM Tag WHERE TagName = v.TagName)
      AND tcm.CategoryID = v.CatID
);
GO

-- Industrials
-- CategoryID: Industrials (look up by name)
-- BA=25, GE=26, CAT=27, MMM=28, F=70, GM=71, TM=72, HMC=73
INSERT INTO TagCategoryMap (TagID, CategoryID)
SELECT t.TagID, c.CategoryID
FROM Tag t
CROSS JOIN Category c
WHERE t.TagName IN ('BA', 'GE', 'CAT', 'MMM', 'F', 'GM', 'TM', 'HMC')
  AND c.CategoryName = 'Industrials'
  AND NOT EXISTS (
      SELECT 1 FROM TagCategoryMap tcm
      WHERE tcm.TagID = t.TagID AND tcm.CategoryID = c.CategoryID
  );
GO

-- Telecommunications
-- CategoryID: Telecommunications (look up by name)
-- T=54, VZ=55, TMUS=56, CMCSA=57
INSERT INTO TagCategoryMap (TagID, CategoryID)
SELECT t.TagID, c.CategoryID
FROM Tag t
CROSS JOIN Category c
WHERE t.TagName IN ('T', 'VZ', 'TMUS', 'CMCSA')
  AND c.CategoryName = 'Telecommunications'
  AND NOT EXISTS (
      SELECT 1 FROM TagCategoryMap tcm
      WHERE tcm.TagID = t.TagID AND tcm.CategoryID = c.CategoryID
  );
GO

-- Media & Streaming (under Telecommunications)
-- NFLX=13 → Media & Streaming
INSERT INTO TagCategoryMap (TagID, CategoryID)
SELECT t.TagID, c.CategoryID
FROM Tag t
CROSS JOIN Category c
WHERE t.TagName = 'NFLX'
  AND c.CategoryName = 'Media & Streaming'
  AND NOT EXISTS (
      SELECT 1 FROM TagCategoryMap tcm
      WHERE tcm.TagID = t.TagID AND tcm.CategoryID = c.CategoryID
  );
GO

-- EV Makers → Technology (CategoryID=1)
-- RIVN=68, LCID=69
INSERT INTO TagCategoryMap (TagID, CategoryID)
SELECT t.TagID, c.CategoryID
FROM Tag t
CROSS JOIN Category c
WHERE t.TagName IN ('RIVN', 'LCID')
  AND c.CategoryID = 1
  AND NOT EXISTS (
      SELECT 1 FROM TagCategoryMap tcm
      WHERE tcm.TagID = t.TagID AND tcm.CategoryID = c.CategoryID
  );
GO

PRINT 'Section C+ done: TagCategoryMap populated.';
GO

-- =====================================================================================
-- SECTION D — BULK NEWS ARTICLES + NEWS TAGS
-- Generates 5000 articles, ~5% CreatedByID=NULL, ~15% Inactive
-- =====================================================================================

DECLARE @NumArticles INT = 5000;
DECLARE @i INT = 1;

DECLARE @CategoryID INT, @TagID INT, @TagName NVARCHAR(50), @StaffID INT, @CreatorId INT;
DECLARE @Decision NVARCHAR(10);
DECLARE @CreatedDate DATETIME;
DECLARE @NewsArticleID INT;
DECLARE @RandVal INT;
DECLARE @SentimentTxt NVARCHAR(500), @FundamentalTxt NVARCHAR(500), @RiskTxt NVARCHAR(500);
DECLARE @IsActiveFlag BIT;

WHILE @i <= @NumArticles
BEGIN
    -- Random Category
    SELECT TOP 1 @CategoryID = CategoryID FROM Category ORDER BY NEWID();

    -- Random Tag
    SELECT TOP 1 @TagID = TagID, @TagName = TagName FROM Tag ORDER BY NEWID();

    -- Random Staff creator
    SELECT TOP 1 @StaffID = AccountID FROM SystemAccount WHERE AccountRole = 1 ORDER BY NEWID();

    -- ~5% mô phỏng account đã bị xoá (CreatedByID = NULL)
    SET @CreatorId = CASE WHEN ABS(CHECKSUM(NEWID())) % 100 < 5 THEN NULL ELSE @StaffID END;

    -- Decision: 40% BUY, 30% SELL, 30% HOLD
    SET @RandVal = ABS(CHECKSUM(NEWID())) % 100;
    SET @Decision = CASE
        WHEN @RandVal < 40 THEN 'BUY'
        WHEN @RandVal < 70 THEN 'SELL'
        ELSE 'HOLD'
    END;

    -- Trải ngẫu nhiên trong 730 ngày gần nhất (theo phút)
    SET @CreatedDate = DATEADD(MINUTE, -(ABS(CHECKSUM(NEWID())) % (730 * 1440)), GETUTCDATE());

    -- ~85% Active, ~15% Inactive (test archive/filter)
    SET @IsActiveFlag = CASE WHEN ABS(CHECKSUM(NEWID())) % 100 < 85 THEN 1 ELSE 0 END;

    SET @SentimentTxt = CASE ABS(CHECKSUM(NEWID())) % 5
        WHEN 0 THEN 'Sentiment view: Market sentiment is broadly positive, driven by strong quarterly performance and favorable analyst commentary.'
        WHEN 1 THEN 'Sentiment view: Sentiment has turned cautious amid mixed signals from recent earnings calls and shifting institutional positioning.'
        WHEN 2 THEN 'Sentiment view: Overall sentiment remains neutral, with investors awaiting further clarity on near-term catalysts.'
        WHEN 3 THEN 'Sentiment view: Negative sentiment prevails following disappointing guidance and increased short interest.'
        ELSE      'Sentiment view: Sentiment is improving steadily as recent news flow skews constructive across financial media.'
    END;

    SET @FundamentalTxt = CASE ABS(CHECKSUM(NEWID())) % 5
        WHEN 0 THEN 'Fundamental view: Revenue growth remains healthy with expanding margins and a solid balance sheet.'
        WHEN 1 THEN 'Fundamental view: Fundamentals show some pressure from rising input costs and competitive dynamics.'
        WHEN 2 THEN 'Fundamental view: Core business metrics are stable, though growth has decelerated from prior quarters.'
        WHEN 3 THEN 'Fundamental view: Strong free cash flow generation supports continued reinvestment and shareholder returns.'
        ELSE      'Fundamental view: Balance sheet leverage has increased, warranting a closer look at debt servicing capacity.'
    END;

    SET @RiskTxt = CASE ABS(CHECKSUM(NEWID())) % 5
        WHEN 0 THEN 'Key risk warnings: Macroeconomic headwinds, including interest rate uncertainty, could weigh on valuation multiples.'
        WHEN 1 THEN 'Key risk warnings: Regulatory scrutiny in key markets poses a potential overhang on future growth.'
        WHEN 2 THEN 'Key risk warnings: Sector-wide competitive pressure may compress margins further in coming quarters.'
        WHEN 3 THEN 'Key risk warnings: Currency fluctuations and supply chain constraints remain notable watch items.'
        ELSE      'Key risk warnings: Broader market volatility could disproportionately affect near-term price action.'
    END;

    INSERT INTO NewsArticle
        (NewsTitle, Headline, CreatedDate, NewsContent, NewsSource,
         CategoryID, NewsStatus, CreatedByID, UpdatedByID, ModifiedDate, ConfidenceScore)
    VALUES
        (
            '[' + @Decision + '] ' + @TagName + ' Automated Analysis',
            @Decision + ' signal generated for ' + @TagName + ' based on synthesized sentiment and fundamental review.',
            @CreatedDate,
            '(1) ' + @SentimentTxt + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
            '(2) ' + @FundamentalTxt + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
            '(3) ' + @RiskTxt,
            'NewsAPI.org + gpt-4o (bulk seed)',
            @CategoryID,
            @IsActiveFlag,
            @CreatorId,
            CASE WHEN @CreatorId IS NOT NULL AND ABS(CHECKSUM(NEWID())) % 100 < 20
                 THEN @CreatorId ELSE NULL END,
            CASE WHEN @CreatorId IS NOT NULL AND ABS(CHECKSUM(NEWID())) % 100 < 20
                 THEN DATEADD(MINUTE, 30, @CreatedDate) ELSE NULL END,
            ABS(CHECKSUM(NEWID())) % 31 + 70
        );

    SET @NewsArticleID = SCOPE_IDENTITY();

    INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (@NewsArticleID, @TagID);

    IF @i % 500 = 0
        PRINT 'Da sinh ' + CAST(@i AS NVARCHAR(10)) + ' / ' + CAST(@NumArticles AS NVARCHAR(10)) + ' NewsArticle...';

    SET @i += 1;
END
GO

PRINT 'Section D done: NewsArticle + NewsTag populated.';
GO

-- =====================================================================================
-- SECTION E — VERIFICATION QUERIES
-- =====================================================================================
SELECT 'SystemAccount' AS TableName, COUNT(*) AS [RowCount] FROM SystemAccount
UNION ALL SELECT 'Category', COUNT(*) FROM Category
UNION ALL SELECT 'Tag', COUNT(*) FROM Tag
UNION ALL SELECT 'NewsArticle', COUNT(*) FROM NewsArticle
UNION ALL SELECT 'NewsTag', COUNT(*) FROM NewsTag
UNION ALL SELECT 'SavedReport', COUNT(*) FROM SavedReport
UNION ALL SELECT 'TagCategoryMap', COUNT(*) FROM TagCategoryMap;

SELECT NewsStatus, COUNT(*) AS Total FROM NewsArticle GROUP BY NewsStatus;

SELECT LEFT(NewsTitle, 6) AS DecisionPrefix, COUNT(*) AS Total
FROM NewsArticle GROUP BY LEFT(NewsTitle, 6);

SELECT MIN(CreatedDate) AS EarliestDate, MAX(CreatedDate) AS LatestDate FROM NewsArticle;

SELECT TOP 10 a.AccountName, COUNT(n.NewsArticleID) AS ReportCount
FROM SystemAccount a
LEFT JOIN NewsArticle n ON n.CreatedByID = a.AccountID
WHERE a.AccountRole = 1
GROUP BY a.AccountName
ORDER BY ReportCount DESC;

-- Test accounts present
SELECT AccountEmail, AccountRole FROM SystemAccount
WHERE AccountEmail IN ('staff@FUNewsTradingSystem.org','lecturer@FUNewsTradingSystem.org');

-- TagCategoryMap spot-check
SELECT t.TagName, c.CategoryName
FROM TagCategoryMap tcm
JOIN Tag t ON t.TagID = tcm.TagID
JOIN Category c ON c.CategoryID = tcm.CategoryID
ORDER BY t.TagName;

PRINT '=== BULK SEED HOAN TAT ===';
GO
