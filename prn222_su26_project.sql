-- =====================================================================================
-- Database Script for FUNewsTradingSystem (FUNewsTradingSystem)
-- Role: P2 - Backend Developer
-- Description: Creates the database schema, tables, relationships, and extended seed data.
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

-- =====================================================================================
-- 2. Extended Seed Data
-- =====================================================================================

-- A. Accounts
-- -------------------------------------------------------------------------------------
INSERT INTO SystemAccount (AccountName, AccountEmail, AccountRole, AccountPassword)
VALUES 
-- Staff (Role 1)
('John Operator', 'john.staff@FUNewsTradingSystem.org', 1, '@@abc123@@_HASH_PLACEHOLDER'),
('Sarah Analyst', 'sarah.staff@FUNewsTradingSystem.org', 1, '@@abc123@@_HASH_PLACEHOLDER'),
('Mike Trader', 'mike.staff@FUNewsTradingSystem.org', 1, '@@abc123@@_HASH_PLACEHOLDER'),
-- Lecturers (Role 2)
('Dr. Alan Smith', 'alan.lecturer@FUNewsTradingSystem.org', 2, '@@abc123@@_HASH_PLACEHOLDER'),
('Prof. Maria Garcia', 'maria.lecturer@FUNewsTradingSystem.org', 2, '@@abc123@@_HASH_PLACEHOLDER');
GO

-- B. Categories (Including sub-categories for hierarchical testing)
-- -------------------------------------------------------------------------------------
-- Top-level categories (IDs 1-6)
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

-- C. Tags (Tickers)
-- -------------------------------------------------------------------------------------
INSERT INTO Tag (TagName, Note) VALUES 
('AAPL', 'Apple Inc.'),         -- 1
('NVDA', 'NVIDIA Corporation'), -- 2
('MSFT', 'Microsoft Corp.'),    -- 3
('GOOGL', 'Alphabet Inc.'),     -- 4
('TSLA', 'Tesla, Inc.'),        -- 5
('BTC', 'Bitcoin'),             -- 6
('ETH', 'Ethereum'),            -- 7
('AMZN', 'Amazon.com, Inc.'),   -- 8
('META', 'Meta Platforms'),     -- 9
('JPM', 'JPMorgan Chase & Co.'),-- 10
('PFE', 'Pfizer Inc.'),         -- 11
('XOM', 'Exxon Mobil Corp.');   -- 12
GO

-- D. News Articles (Mocked AI Pipeline outputs for UI testing)
-- -------------------------------------------------------------------------------------
INSERT INTO NewsArticle (NewsTitle, Headline, CreatedDate, NewsContent, NewsSource, CategoryID, NewsStatus, CreatedByID, UpdatedByID, ModifiedDate) VALUES 
(
    '[BUY] NVDA Automated Analysis', 
    'NVIDIA maintains market dominance with next-generation AI chip announcements.', 
    DATEADD(day, -2, GETUTCDATE()), 
    '(1) Sentiment: Highly positive across all financial media due to unexpected revenue beats. (2) Fundamental: Unprecedented pricing power and profit margins with their latest Blackwell architecture. (3) Risks: Geopolitical tensions affecting Taiwan supply chains.', 
    'NewsAPI.org + gpt-4o', 
    8, -- Semiconductors
    1, 1, 1, DATEADD(day, -2, GETUTCDATE())
),
(
    '[SELL] TSLA Automated Analysis', 
    'Increasing competition in EV space and shrinking margins trigger concerns.', 
    DATEADD(day, -5, GETUTCDATE()), 
    '(1) Sentiment: Negative to neutral, reflecting investor anxiety over recent price cuts. (2) Fundamental: Margins are compressing as the company prioritizes volume over profitability. (3) Risks: Rising interest rates making auto financing difficult and delays in autonomous driving tech.', 
    'NewsAPI.org + gpt-4o', 
    6, -- Consumer Goods
    1, 2, 2, DATEADD(day, -5, GETUTCDATE())
),
(
    '[HOLD] AAPL Automated Analysis', 
    'Steady services growth offsets sluggish hardware cycle amidst regulatory scrutiny.', 
    DATEADD(day, -10, GETUTCDATE()), 
    '(1) Sentiment: Neutral. The market is awaiting the next major iPhone upgrade cycle. (2) Fundamental: Incredible cash generation and share buybacks provide a high floor, but growth is currently stagnant. (3) Risks: EU regulatory fines and anti-trust lawsuits impacting App Store revenue.', 
    'NewsAPI.org + gpt-4o', 
    1, -- Technology (Parent)
    1, 1, 1, DATEADD(day, -10, GETUTCDATE())
),
(
    '[BUY] BTC Automated Analysis', 
    'Institutional adoption accelerates following successful ETF launches.', 
    DATEADD(day, -1, GETUTCDATE()), 
    '(1) Sentiment: Overwhelmingly positive as major financial institutions allocate capital. (2) Fundamental: Supply shock post-halving combined with sustained ETF inflows creates strong upward pressure. (3) Risks: Sudden macroeconomic shifts or harsh regulatory crackdowns in major jurisdictions.', 
    'NewsAPI.org + gpt-4o', 
    5, -- Cryptocurrencies
    1, 3, 3, DATEADD(day, -1, GETUTCDATE())
),
(
    '[SELL] PFE Automated Analysis', 
    'Post-pandemic revenue slump continues to weigh heavily on valuation.', 
    DATEADD(day, -15, GETUTCDATE()), 
    '(1) Sentiment: Negative. Investors are rotating out of the stock due to a lack of immediate catalysts. (2) Fundamental: Steep drop-off in vaccine and antiviral revenues with a thin near-term pipeline. (3) Risks: Failure in upcoming phase 3 trials could further depress the stock.', 
    'NewsAPI.org + gpt-4o', 
    9, -- Biotechnology
    0, -- INACTIVE (To test Guest/Lecturer list filtering)
    2, 2, DATEADD(day, -1, GETUTCDATE())
),
(
    '[HOLD] AAPL Automated Analysis', 
    'Steady services growth offsets sluggish hardware cycle amidst regulatory scrutiny.', 
    DATEADD(day, -10, GETUTCDATE()), 
    '(1) Sentiment: Neutral. The market is awaiting the next major iPhone upgrade cycle. (2) Fundamental: Incredible cash generation and share buybacks provide a high floor, but growth is currently stagnant. (3) Risks: EU regulatory fines and anti-trust lawsuits impacting App Store revenue.', 
    'NewsAPI.org + gpt-4o', 
    1, -- Technology (Parent)
    1, 1, 1, DATEADD(day, -10, GETUTCDATE())
),
(
    '[BUY] MSFT Automated Analysis', 
    'Microsoft maintains market dominance with next-generation AI chip announcements.', 
    DATEADD(day, -2, GETUTCDATE()), 
    '(1) Sentiment: Highly positive across all financial media due to unexpected revenue beats. (2) Fundamental: Unprecedented pricing power and profit margins with their latest Blackwell architecture. (3) Risks: Geopolitical tensions affecting Taiwan supply chains.', 
    'NewsAPI.org + gpt-4o', 
    8, -- Semiconductors
    1, 1, 1, DATEADD(day, -2, GETUTCDATE())
);
GO

-- E. NewsTags (Mapping Articles to Tickers)
-- -------------------------------------------------------------------------------------
-- Article 1 -> NVDA
INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (1, 2);
-- Article 2 -> TSLA
INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (2, 5);
-- Article 3 -> AAPL
INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (3, 1);
-- Article 4 -> BTC
INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (4, 6);
-- Article 5 -> PFE
INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (5, 11);
GO

PRINT 'FUNewsTradingSystem Database Schema and Extended Seed Data deployed successfully!';
GO

-- =====================================================================================
-- FUNewsTradingSystem — BULK TEST DATA EXPANSION
-- =====================================================================================
-- Mục đích : Mở rộng dữ liệu test với khối lượng lớn để kiểm thử thực tế:
--            - Pagination (10 dòng/trang) trên toàn bộ list view
--            - Admin Statistics (lọc theo khoảng ngày) trải dài ~2 năm
--            - Staff Report History (nhiều Staff, nhiều report/người)
--            - Report Viewer public (Active/Inactive, nhiều Category/Tag)
--            - "Deleted User" hiển thị khi CreatedByID = NULL
-- =====================================================================================

PRINT '=== BẮT ĐẦU BULK SEED ===';
GO

-- =====================================================================================
-- SECTION A — BULK ACCOUNTS
-- Thêm 80 Staff (Role=1) + 40 Lecturer (Role=2) => tổng ~120 account mới
-- Dùng để test: nhiều Staff khác nhau tạo report (Staff History phân biệt theo người),
-- Admin Account list có pagination thật sự (page size = 10).
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

PRINT 'Section A xong: đã thêm accounts.';
GO

-- =====================================================================================
-- SECTION B — BULK CATEGORIES
-- Thêm ~28 category mới: 5 sector cấp cao mới + nhiều sub-category dưới các sector
-- có sẵn (Technology=1, Healthcare=2, Finance=3, Energy=4, Cryptocurrencies=5,
-- Consumer Goods=6). Có kèm 1 category IsActive=0 để test loại khỏi dropdown Run Analysis.
-- =====================================================================================

-- 5 sector cấp cao mới
INSERT INTO Category (CategoryName, CategoryDescription, IsActive) VALUES
('Industrials', 'Manufacturing, aerospace, and industrial machinery', 1),
('Telecommunications', 'Wireless carriers, media and streaming infrastructure', 1),
('Real Estate', 'REITs and property investment vehicles', 1),
('Utilities', 'Electric, water, and gas utility providers', 1),
('Materials', 'Mining, metals, and raw materials producers', 1),
('Deprecated Sector (Test)', 'Retired sector kept for IsActive=0 dropdown exclusion test', 0);
GO

-- Sub-category dưới Technology (CategoryID = 1)
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

-- Sub-category dưới Healthcare (CategoryID = 2)
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

-- Sub-category dưới Finance (CategoryID = 3)
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

-- Sub-category dưới Energy (CategoryID = 4)
INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT v.Name, v.Descr, c.CategoryID, 1
FROM Category c
CROSS APPLY (VALUES
    ('Oil & Gas Exploration', 'Upstream exploration and production'),
    ('Nuclear Energy', 'Nuclear power generation and fuel supply')
) AS v(Name, Descr)
WHERE c.CategoryName = 'Energy';
GO

-- Sub-category dưới Cryptocurrencies (CategoryID = 5)
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

-- Sub-category dưới Consumer Goods (CategoryID = 6)
INSERT INTO Category (CategoryName, CategoryDescription, ParentCategoryID, IsActive)
SELECT v.Name, v.Descr, c.CategoryID, 1
FROM Category c
CROSS APPLY (VALUES
    ('Apparel & Footwear', 'Clothing and footwear brands'),
    ('Food & Beverage', 'Packaged food and beverage producers')
) AS v(Name, Descr)
WHERE c.CategoryName = 'Consumer Goods';
GO

-- Sub-category dưới các sector mới
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

PRINT 'Section B xong: đã thêm categories.';
GO

-- =====================================================================================
-- SECTION C — BULK TAGS (thêm ~90 ticker thực tế, tổng cộng ~102 tags)
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
    ('LTC','Litecoin'), ('DOT','Polkadot'), ('LINK','Chainlink'),
    ('XOM','Exxon Mobil Corporation (dup guard)')
) AS v(TagName, Note)
WHERE NOT EXISTS (SELECT 1 FROM Tag t WHERE t.TagName = v.TagName);
GO

PRINT 'Section C xong: đã thêm tags.';
GO

-- =====================================================================================
-- SECTION D — BULK NEWS ARTICLES + NEWSTAG
-- Sinh @NumArticles report giả lập, trải ngẫu nhiên trong ~730 ngày (2 năm) qua,
-- gán ngẫu nhiên Category / Tag / Staff, decision BUY/SELL/HOLD, ~15% Inactive,
-- ~5% CreatedByID = NULL (mô phỏng "Deleted User").
-- Có thể chỉnh @NumArticles để tăng/giảm khối lượng (mặc định 5000 ~ x700 so với
-- 7 dòng gốc trong prn222_su26_project.sql).
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

    -- Trải ngẫu nhiên trong 730 ngày gần nhất (theo phút, để trùng lặp ít)
    SET @CreatedDate = DATEADD(MINUTE, -(ABS(CHECKSUM(NEWID())) % (730 * 1440)), GETUTCDATE());

    -- ~85% Active, ~15% Inactive (test archive/filter)
    SET @IsActiveFlag = CASE WHEN ABS(CHECKSUM(NEWID())) % 100 < 85 THEN 1 ELSE 0 END;

    -- Nội dung: chọn ngẫu nhiên 1 trong 5 biến thể mỗi phần để có sự đa dạng
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
         CategoryID, NewsStatus, CreatedByID, UpdatedByID, ModifiedDate)
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
                 THEN DATEADD(MINUTE, 30, @CreatedDate) ELSE NULL END
        );

    SET @NewsArticleID = SCOPE_IDENTITY();

    INSERT INTO NewsTag (NewsArticleID, TagID) VALUES (@NewsArticleID, @TagID);

    IF @i % 500 = 0
        PRINT 'Đã sinh ' + CAST(@i AS NVARCHAR(10)) + ' / ' + CAST(@NumArticles AS NVARCHAR(10)) + ' NewsArticle...';

    SET @i += 1;
END
GO

PRINT 'Section D xong: đã thêm NewsArticle + NewsTag.';
GO

-- =====================================================================================
-- SECTION E — VERIFICATION QUERIES (chạy để kiểm tra kết quả sau khi seed)
-- =====================================================================================
SELECT 'SystemAccount' AS TableName, COUNT(*) AS [RowCount] FROM SystemAccount
UNION ALL SELECT 'Category', COUNT(*) FROM Category
UNION ALL SELECT 'Tag', COUNT(*) FROM Tag
UNION ALL SELECT 'NewsArticle', COUNT(*) FROM NewsArticle
UNION ALL SELECT 'NewsTag', COUNT(*) FROM NewsTag;

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

PRINT '=== BULK SEED HOÀN TẤT ===';
GO