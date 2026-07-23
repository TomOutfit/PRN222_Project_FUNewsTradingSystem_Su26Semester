-- =====================================================================================
-- Database Script for FUNewsTradingSystem (FUNewsTradingSystem)
-- Role: P2 - Backend Developer
-- Description: Creates the database schema, tables, relationships, and extended seed data.
-- Target RDBMS: PostgreSQL
-- =====================================================================================

-- Drop & Create Database
-- (Run via: psql -U postgres -h localhost -c "CREATE DATABASE fnts;" first if needed)
-- For Render PostgreSQL, database is created automatically. This script starts from tables.

-- =====================================================================================
-- 1. Drop existing tables (in correct order due to FK constraints)
-- =====================================================================================
DROP TABLE IF EXISTS "SavedReport" CASCADE;
DROP TABLE IF EXISTS "TagCategoryMap" CASCADE;
DROP TABLE IF EXISTS "NewsTag" CASCADE;
DROP TABLE IF EXISTS "NewsArticle" CASCADE;
DROP TABLE IF EXISTS "Tag" CASCADE;
DROP TABLE IF EXISTS "Category" CASCADE;
DROP TABLE IF EXISTS "SystemAccount" CASCADE;

-- =====================================================================================
-- 2. Create Tables
-- =====================================================================================

-- Table: SystemAccount
CREATE TABLE "SystemAccount" (
    "AccountID" SERIAL PRIMARY KEY,
    "AccountName" VARCHAR(100) NOT NULL,
    "AccountEmail" VARCHAR(200) NOT NULL,
    "AccountRole" INTEGER NOT NULL, -- 1=Staff, 2=Lecturer, 3=Admin
    "AccountPassword" VARCHAR(500) NOT NULL,
    CONSTRAINT "UQ_SystemAccount_AccountEmail" UNIQUE ("AccountEmail")
);

-- Table: Category
CREATE TABLE "Category" (
    "CategoryID" SERIAL PRIMARY KEY,
    "CategoryName" VARCHAR(200) NOT NULL,
    "CategoryDescription" VARCHAR(500),
    "ParentCategoryID" INTEGER,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "FK_Category_ParentCategory" FOREIGN KEY ("ParentCategoryID")
        REFERENCES "Category"("CategoryID")
        ON DELETE NO ACTION
);

-- Table: Tag
CREATE TABLE "Tag" (
    "TagID" SERIAL PRIMARY KEY,
    "TagName" VARCHAR(50) NOT NULL,
    "Note" VARCHAR(500),
    CONSTRAINT "UQ_Tag_TagName" UNIQUE ("TagName")
);

-- Table: NewsArticle
CREATE TABLE "NewsArticle" (
    "NewsArticleID" SERIAL PRIMARY KEY,
    "NewsTitle" VARCHAR(500) NOT NULL,
    "Headline" VARCHAR(1000) NOT NULL,
    "CreatedDate" TIMESTAMP NOT NULL,
    "NewsContent" TEXT NOT NULL,
    "NewsSource" VARCHAR(500),
    "CategoryID" INTEGER NOT NULL,
    "NewsStatus" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedByID" INTEGER,
    "UpdatedByID" INTEGER,
    "ModifiedDate" TIMESTAMP,
    "ConfidenceScore" INTEGER,
    CONSTRAINT "FK_NewsArticle_Category" FOREIGN KEY ("CategoryID")
        REFERENCES "Category"("CategoryID")
        ON DELETE NO ACTION,
    CONSTRAINT "FK_NewsArticle_CreatedBy" FOREIGN KEY ("CreatedByID")
        REFERENCES "SystemAccount"("AccountID")
        ON DELETE SET NULL,
    CONSTRAINT "FK_NewsArticle_UpdatedBy" FOREIGN KEY ("UpdatedByID")
        REFERENCES "SystemAccount"("AccountID")
        ON DELETE NO ACTION
);

-- Table: NewsTag (Junction Table)
CREATE TABLE "NewsTag" (
    "NewsArticleID" INTEGER NOT NULL,
    "TagID" INTEGER NOT NULL,
    PRIMARY KEY ("NewsArticleID", "TagID"),
    CONSTRAINT "FK_NewsTag_NewsArticle" FOREIGN KEY ("NewsArticleID")
        REFERENCES "NewsArticle"("NewsArticleID")
        ON DELETE CASCADE,
    CONSTRAINT "FK_NewsTag_Tag" FOREIGN KEY ("TagID")
        REFERENCES "Tag"("TagID")
        ON DELETE NO ACTION
);

-- Table: SavedReport
CREATE TABLE "SavedReport" (
    "SavedReportID" SERIAL PRIMARY KEY,
    "AccountID" INTEGER NOT NULL,
    "NewsArticleID" INTEGER NOT NULL,
    "SavedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "Notes" VARCHAR(1000),
    CONSTRAINT "FK_SavedReport_SystemAccount" FOREIGN KEY ("AccountID")
        REFERENCES "SystemAccount"("AccountID")
        ON DELETE CASCADE,
    CONSTRAINT "FK_SavedReport_NewsArticle" FOREIGN KEY ("NewsArticleID")
        REFERENCES "NewsArticle"("NewsArticleID")
        ON DELETE CASCADE,
    CONSTRAINT "UQ_SavedReport_AccountArticle" UNIQUE ("AccountID", "NewsArticleID")
);

-- Table: TagCategoryMap
CREATE TABLE "TagCategoryMap" (
    "TagCategoryMapID" SERIAL PRIMARY KEY,
    "TagID" INTEGER NOT NULL,
    "CategoryID" INTEGER NOT NULL,
    CONSTRAINT "FK_TagCategoryMap_Tag" FOREIGN KEY ("TagID")
        REFERENCES "Tag"("TagID")
        ON DELETE CASCADE,
    CONSTRAINT "FK_TagCategoryMap_Category" FOREIGN KEY ("CategoryID")
        REFERENCES "Category"("CategoryID")
        ON DELETE CASCADE,
    CONSTRAINT "UQ_TagCategoryMap_TagCategory" UNIQUE ("TagID", "CategoryID")
);

-- =====================================================================================
-- 3. Seed Data — Base Accounts
-- =====================================================================================
INSERT INTO "SystemAccount" ("AccountName", "AccountEmail", "AccountRole", "AccountPassword")
VALUES
    ('John Operator', 'john.staff@FUNewsTradingSystem.org', 1, '@@abc123@@_HASH_PLACEHOLDER'),
    ('Sarah Analyst', 'sarah.staff@FUNewsTradingSystem.org', 1, '@@abc123@@_HASH_PLACEHOLDER'),
    ('Mike Trader', 'mike.staff@FUNewsTradingSystem.org', 1, '@@abc123@@_HASH_PLACEHOLDER'),
    ('Dr. Alan Smith', 'alan.lecturer@FUNewsTradingSystem.org', 2, '@@abc123@@_HASH_PLACEHOLDER'),
    ('Prof. Maria Garcia', 'maria.lecturer@FUNewsTradingSystem.org', 2, '@@abc123@@_HASH_PLACEHOLDER');

-- EF Core test accounts (mirrored for prod/demo testing)
INSERT INTO "SystemAccount" ("AccountName", "AccountEmail", "AccountRole", "AccountPassword")
VALUES
    ('Test Staff',    'staff@FUNewsTradingSystem.org',    1, '@@abc123@@_HASH_PLACEHOLDER'),
    ('Test Lecturer', 'lecturer@FUNewsTradingSystem.org', 2, '@@abc123@@_HASH_PLACEHOLDER');

-- =====================================================================================
-- 4. Seed Data — Categories
-- =====================================================================================
-- Top-level categories
INSERT INTO "Category" ("CategoryName", "CategoryDescription", "IsActive") VALUES
('Technology', 'Technology sector including software, hardware, and IT services', TRUE),
('Healthcare', 'Healthcare sector including pharmaceuticals and medical devices', TRUE),
('Finance', 'Financial sector including banking and investment', TRUE),
('Energy', 'Energy sector including oil, gas, and renewable energy', TRUE),
('Cryptocurrencies', 'Digital assets and blockchain technology', TRUE),
('Consumer Goods', 'Goods bought and used by consumers', TRUE);

-- Sub-categories
INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive") VALUES
('Software & AI', 'Companies developing software solutions and artificial intelligence', 1, TRUE),
('Semiconductors', 'Hardware manufacturers of computer chips and GPUs', 1, TRUE),
('Biotechnology', 'Companies applying biological processes to manufacturing', 2, TRUE),
('Commercial Banking', 'Traditional consumer and business banking institutions', 3, TRUE),
('Green Energy', 'Renewable resources such as solar and wind', 4, TRUE),
('E-commerce', 'Online retail and distribution', 6, TRUE);

-- =====================================================================================
-- 5. Seed Data — Tags (Tickers)
-- =====================================================================================
INSERT INTO "Tag" ("TagName", "Note") VALUES
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

-- =====================================================================================
-- 6. Seed Data — News Articles
-- =====================================================================================
INSERT INTO "NewsArticle" ("NewsTitle", "Headline", "CreatedDate", "NewsContent", "NewsSource", "CategoryID", "NewsStatus", "CreatedByID", "UpdatedByID", "ModifiedDate")
VALUES
('[BUY] NVDA Automated Analysis',
 'NVIDIA maintains market dominance with next-generation AI chip announcements.',
 NOW() - INTERVAL '2 days',
 '(1) Sentiment: Highly positive across all financial media due to unexpected revenue beats. (2) Fundamental: Unprecedented pricing power and profit margins with their latest Blackwell architecture. (3) Risks: Geopolitical tensions affecting Taiwan supply chains.',
 'NewsAPI.org + gpt-4o',
 8, TRUE, 1, 1, NOW() - INTERVAL '2 days'),

('[SELL] TSLA Automated Analysis',
 'Increasing competition in EV space and shrinking margins trigger concerns.',
 NOW() - INTERVAL '5 days',
 '(1) Sentiment: Negative to neutral, reflecting investor anxiety over recent price cuts. (2) Fundamental: Margins are compressing as the company prioritizes volume over profitability. (3) Risks: Rising interest rates making auto financing difficult and delays in autonomous driving tech.',
 'NewsAPI.org + gpt-4o',
 6, TRUE, 2, 2, NOW() - INTERVAL '5 days'),

('[HOLD] AAPL Automated Analysis',
 'Steady services growth offsets sluggish hardware cycle amidst regulatory scrutiny.',
 NOW() - INTERVAL '10 days',
 '(1) Sentiment: Neutral. The market is awaiting the next major iPhone upgrade cycle. (2) Fundamental: Incredible cash generation and share buybacks provide a high floor, but growth is currently stagnant. (3) Risks: EU regulatory fines and anti-trust lawsuits impacting App Store revenue.',
 'NewsAPI.org + gpt-4o',
 1, TRUE, 1, 1, NOW() - INTERVAL '10 days'),

('[BUY] BTC Automated Analysis',
 'Institutional adoption accelerates following successful ETF launches.',
 NOW() - INTERVAL '1 day',
 '(1) Sentiment: Overwhelmingly positive as major financial institutions allocate capital. (2) Fundamental: Supply shock post-halving combined with sustained ETF inflows creates strong upward pressure. (3) Risks: Sudden macroeconomic shifts or harsh regulatory crackdowns in major jurisdictions.',
 'NewsAPI.org + gpt-4o',
 5, TRUE, 3, 3, NOW() - INTERVAL '1 day'),

('[SELL] PFE Automated Analysis',
 'Post-pandemic revenue slump continues to weigh heavily on valuation.',
 NOW() - INTERVAL '15 days',
 '(1) Sentiment: Negative. Investors are rotating out of the stock due to a lack of immediate catalysts. (2) Fundamental: Steep drop-off in vaccine and antiviral revenues with a thin near-term pipeline. (3) Risks: Failure in upcoming phase 3 trials could further depress the stock.',
 'NewsAPI.org + gpt-4o',
 9, FALSE, 2, 2, NOW() - INTERVAL '1 day'),

('[HOLD] AAPL Automated Analysis',
 'Steady services growth offsets sluggish hardware cycle amidst regulatory scrutiny.',
 NOW() - INTERVAL '10 days',
 '(1) Sentiment: Neutral. The market is awaiting the next major iPhone upgrade cycle. (2) Fundamental: Incredible cash generation and share buybacks provide a high floor, but growth is currently stagnant. (3) Risks: EU regulatory fines and anti-trust lawsuits impacting App Store revenue.',
 'NewsAPI.org + gpt-4o',
 1, TRUE, 1, 1, NOW() - INTERVAL '10 days'),

('[BUY] MSFT Automated Analysis',
 'Microsoft maintains market dominance with next-generation AI chip announcements.',
 NOW() - INTERVAL '2 days',
 '(1) Sentiment: Highly positive across all financial media due to unexpected revenue beats. (2) Fundamental: Unprecedented pricing power and profit margins with their latest Blackwell architecture. (3) Risks: Geopolitical tensions affecting Taiwan supply chains.',
 'NewsAPI.org + gpt-4o',
 8, TRUE, 1, 1, NOW() - INTERVAL '2 days');

-- =====================================================================================
-- 7. Seed Data — NewsTags
-- =====================================================================================
INSERT INTO "NewsTag" ("NewsArticleID", "TagID") VALUES (1, 2);  -- NVDA
INSERT INTO "NewsTag" ("NewsArticleID", "TagID") VALUES (2, 5);  -- TSLA
INSERT INTO "NewsTag" ("NewsArticleID", "TagID") VALUES (3, 1);  -- AAPL
INSERT INTO "NewsTag" ("NewsArticleID", "TagID") VALUES (4, 6);  -- BTC
INSERT INTO "NewsTag" ("NewsArticleID", "TagID") VALUES (5, 11); -- PFE

-- =====================================================================================
-- 8. Seed Data — TagCategoryMap (full mapping: all 105 tags → correct sector)
-- Base CategoryIDs (fixed SERIAL order from Section 4):
--   1=Technology, 2=Healthcare, 3=Finance, 4=Energy, 5=Cryptocurrencies, 6=Consumer Goods
--   7=Software & AI, 8=Semiconductors, 9=Biotechnology, 10=Commercial Banking
--   11=Green Energy, 12=E-commerce
-- Base TagIDs (Section 5, fixed order):
--   1=AAPL,2=NVDA,3=MSFT,4=GOOGL,5=TSLA,6=BTC,7=ETH,8=AMZN,9=META,10=JPM,11=PFE,12=XOM
-- Bulk TagIDs (Section C, order of VALUES list, starting at 13):
--   13=NFLX,14=DIS,15=KO,16=PEP,17=MCD,18=SBUX,19=NKE,20=WMT,21=TGT,22=COST,
--   23=HD,24=LOW,25=BA,26=GE,27=CAT,28=MMM,29=IBM,30=INTC,31=AMD,32=QCOM,
--   33=ORCL,34=CRM,35=ADBE,36=PYPL,37=V,38=MA,39=BAC,40=WFC,41=GS,42=MS,
--   43=C,44=UNH,45=JNJ,46=MRNA,47=LLY,48=ABBV,49=CVX,50=COP,51=SLB,
--   52=NEE,53=DUK,54=T,55=VZ,56=TMUS,57=CMCSA,58=SONY,59=SNAP,60=PINS,
--   61=UBER,62=LYFT,63=ABNB,64=DASH,65=SHOP,66=SQ,67=COIN,68=RIVN,69=LCID,
--   70=F,71=GM,72=TM,73=HMC,74=SPOT,75=RBLX,76=U,77=PLTR,78=SNOW,79=DDOG,
--   80=NET,81=ZM,82=DOCU,83=TEAM,84=NOW,85=WDAY,86=PANW,87=CRWD,88=OKTA,
--   89=MDB,90=TWLO,91=ETSY,92=EBAY,93=BABA,94=JD,95=PDD,96=SE,97=MELI,
--   98=SOL,99=ADA,100=DOGE,101=XRP,102=BNB,103=LTC,104=DOT,105=LINK
-- =====================================================================================
-- TagCategoryMap mappings — moved to after Section C (bulk tags must exist first)
-- See Section C+ below for the full INSERT block

-- =====================================================================================
-- BULK TEST DATA EXPANSION
-- Purpose: Test pagination, admin statistics, staff report history, and filtering
-- =====================================================================================

-- RAISE NOTICE '=== BULK SEED: Starting...';

-- =====================================================================================
-- SECTION A — BULK ACCOUNTS (80 Staff + 40 Lecturer = 120)
-- =====================================================================================
WITH names AS (
    SELECT 
        ROW_NUMBER() OVER () as r,
        f.val as first_name,
        m.val as middle_name,
        l.val as last_name
    FROM (VALUES
        ('Nguyen'), ('Tran'), ('Le'), ('Pham'), ('Hoang'),
        ('Huynh'), ('Phan'), ('Vu'), ('Vo'), ('Dang'),
        ('Bui'), ('Do'), ('Ho'), ('Ngo'), ('Duong'), ('Ly')
    ) AS f(val)
    CROSS JOIN (VALUES
        ('Van'), ('Thi'), ('Minh'), ('Anh'), ('Duc'),
        ('Huy'), ('Hai'), ('Quoc'), ('Hoai'), ('Thanh')
    ) AS m(val)
    CROSS JOIN (VALUES
        ('Anh'), ('Bao'), ('Cuong'), ('Dung'), ('Hung'),
        ('Khoa'), ('Linh'), ('Nam'), ('Phong'), ('Son'),
        ('Trang'), ('Tuan'), ('Viet'), ('Vy'), ('Yen'), ('Khanh')
    ) AS l(val)
)
INSERT INTO "SystemAccount" ("AccountName", "AccountEmail", "AccountRole", "AccountPassword")
SELECT
    first_name || ' ' || middle_name || ' ' || last_name,
    CASE WHEN r <= 80
         THEN LOWER(last_name || '.' || first_name || r || '.staff@FUNewsTradingSystem.org')
         ELSE LOWER(last_name || '.' || first_name || (r - 80) || '.lecturer@FUNewsTradingSystem.org')
    END,
    CASE WHEN r <= 80 THEN 1 ELSE 2 END,
    '@@abc123@@_HASH_PLACEHOLDER'
FROM names
WHERE r <= 120;


-- RAISE NOTICE 'Section A done: accounts added.';

-- =====================================================================================
-- SECTION B — BULK CATEGORIES
-- =====================================================================================
INSERT INTO "Category" ("CategoryName", "CategoryDescription", "IsActive") VALUES
('Industrials', 'Manufacturing, aerospace, and industrial machinery', TRUE),
('Telecommunications', 'Wireless carriers, media and streaming infrastructure', TRUE),
('Real Estate', 'REITs and property investment vehicles', TRUE),
('Utilities', 'Electric, water, and gas utility providers', TRUE),
('Materials', 'Mining, metals, and raw materials producers', TRUE),
('Deprecated Sector (Test)', 'Retired sector kept for IsActive=0 dropdown exclusion test', FALSE);

-- Sub-categories under Technology (CategoryID=1)
INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive")
SELECT v.name, v.descr, c."CategoryID", TRUE
FROM "Category" c
CROSS JOIN (VALUES
    ('Cloud Computing', 'Cloud infrastructure and SaaS platforms'),
    ('Cybersecurity', 'Network and endpoint security vendors'),
    ('Social Media', 'Consumer social networking platforms')
) AS v(name, descr)
WHERE c."CategoryName" = 'Technology';

-- Sub-categories under Healthcare (CategoryID=2)
INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive")
SELECT v.name, v.descr, c."CategoryID", TRUE
FROM "Category" c
CROSS JOIN (VALUES
    ('Pharmaceuticals', 'Drug development and manufacturing'),
    ('Medical Devices', 'Diagnostic and surgical device makers'),
    ('Digital Health', 'Telehealth and health-tech platforms')
) AS v(name, descr)
WHERE c."CategoryName" = 'Healthcare';

-- Sub-categories under Finance (CategoryID=3)
INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive")
SELECT v.name, v.descr, c."CategoryID", TRUE
FROM "Category" c
CROSS JOIN (VALUES
    ('Investment Banking', 'M&A advisory and capital markets'),
    ('Insurance', 'Life, property, and casualty insurers'),
    ('Fintech', 'Digital payments and lending platforms')
) AS v(name, descr)
WHERE c."CategoryName" = 'Finance';

-- Sub-categories under Energy (CategoryID=4)
INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive")
SELECT v.name, v.descr, c."CategoryID", TRUE
FROM "Category" c
CROSS JOIN (VALUES
    ('Oil & Gas Exploration', 'Upstream exploration and production'),
    ('Nuclear Energy', 'Nuclear power generation and fuel supply')
) AS v(name, descr)
WHERE c."CategoryName" = 'Energy';

-- Sub-categories under Cryptocurrencies (CategoryID=5)
INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive")
SELECT v.name, v.descr, c."CategoryID", TRUE
FROM "Category" c
CROSS JOIN (VALUES
    ('DeFi', 'Decentralized finance protocols and tokens'),
    ('NFT & Gaming Tokens', 'Gaming and non-fungible token ecosystems'),
    ('Stablecoins', 'Fiat-pegged digital currencies')
) AS v(name, descr)
WHERE c."CategoryName" = 'Cryptocurrencies';

-- Sub-categories under Consumer Goods (CategoryID=6)
INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive")
SELECT v.name, v.descr, c."CategoryID", TRUE
FROM "Category" c
CROSS JOIN (VALUES
    ('Apparel & Footwear', 'Clothing and footwear brands'),
    ('Food & Beverage', 'Packaged food and beverage producers')
) AS v(name, descr)
WHERE c."CategoryName" = 'Consumer Goods';

-- Sub-categories under new sectors
INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive")
SELECT v.name, v.descr, c."CategoryID", TRUE
FROM "Category" c
CROSS JOIN (VALUES
    ('Aerospace & Defense', 'Aircraft and defense contractors'),
    ('Industrial Machinery', 'Heavy equipment and automation')
) AS v(name, descr)
WHERE c."CategoryName" = 'Industrials';

INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive")
SELECT v.name, v.descr, c."CategoryID", TRUE
FROM "Category" c
CROSS JOIN (VALUES
    ('Wireless Carriers', 'Mobile network operators'),
    ('Media & Streaming', 'Content production and distribution')
) AS v(name, descr)
WHERE c."CategoryName" = 'Telecommunications';

INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive")
SELECT 'REITs', 'Real estate investment trusts', c."CategoryID", TRUE
FROM "Category" c WHERE c."CategoryName" = 'Real Estate';

INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive")
SELECT 'Electric Utilities', 'Regulated electric power providers', c."CategoryID", TRUE
FROM "Category" c WHERE c."CategoryName" = 'Utilities';

INSERT INTO "Category" ("CategoryName", "CategoryDescription", "ParentCategoryID", "IsActive")
SELECT 'Mining & Metals', 'Metal ore extraction and processing', c."CategoryID", TRUE
FROM "Category" c WHERE c."CategoryName" = 'Materials';

-- RAISE NOTICE 'Section B done: categories added.';

-- =====================================================================================
-- SECTION C — BULK TAGS (90+ new tickers)
-- =====================================================================================
INSERT INTO "Tag" ("TagName", "Note")
SELECT v."TagName", v."Note"
FROM (VALUES
    ('NFLX','Netflix, Inc.'), ('DIS','The Walt Disney Company'), ('KO','The Coca-Cola Company'),
    ('PEP','PepsiCo, Inc.'), ('MCD','McDonald''s Corporation'), ('SBUX','Starbucks Corporation'),
    ('NKE','Nike, Inc.'), ('WMT','Walmart Inc.'), ('TGT','Target Corporation'),
    ('COST','Costco Wholesale Corporation'), ('HD','The Home Depot, Inc.'), ('LOW','Lowe''s Companies, Inc.'),
    ('BA','The Boeing Company'), ('GE','General Electric Company'), ('CAT','Caterpillar Inc.'),
    ('MMM','3M Company'), ('IBM','International Business Machines Corporation'), ('INTC','Intel Corporation'),
    ('AMD','Advanced Micro Devices, Inc.'), ('QCOM','QualCOMM Incorporated'), ('ORCL','Oracle Corporation'),
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
    -- ── Vietnamese Stock Market (HOSE / HNX / VN30) ──────────────────────
    ('FPT','FPT Corporation (Tập đoàn FPT)'),
    ('VNM','Vinamilk (Công ty CP Sữa Việt Nam)'),
    ('VIC','Vingroup Joint Stock Company (Tập đoàn Vingroup)'),
    ('VHM','Vinhomes Joint Stock Company (Công ty CP Vinhomes)'),
    ('VCB','Vietcombank (Ngân hàng TMCP Ngoại thương Việt Nam)'),
    ('BID','BIDV (Ngân hàng TMCP Đầu tư và Phát triển Việt Nam)'),
    ('CTG','VietinBank (Ngân hàng TMCP Công thương Việt Nam)'),
    ('TCB','Techcombank (Ngân hàng TMCP Kỹ thương Việt Nam)'),
    ('MBB','MBBank (Ngân hàng TMCP Quân đội)'),
    ('VPB','VPBank (Ngân hàng TMCP Việt Nam Thịnh Vượng)'),
    ('HPG','Hoa Phat Group (Tập đoàn Hòa Phát)'),
    ('MWG','Mobile World Investment Corp (Thế Giới Di Động)'),
    ('MSN','Masan Group (Tập đoàn Masan)'),
    ('GAS','PV GAS (Tổng Công ty Khí Việt Nam)'),
    ('PLX','Petrolimex (Tập đoàn Xăng dầu Việt Nam)'),
    ('VJC','VietJet Aviation JSC (VietJet Air)'),
    ('HVN','Vietnam Airlines (Tổng Công ty Hàng không Việt Nam)'),
    ('SSI','SSI Securities Corporation (Chứng khoán SSI)'),
    ('VND','VNDIRECT Securities Corporation (Chứng khoán VNDIRECT)'),
    ('REE','Refrigeration Electrical Engineering (Cơ Điện Lạnh)'),
    ('PNJ','Phu Nhuan Jewelry JSC (Vàng bạc Đá quý Phú Nhuận)'),
    ('STB','Sacombank (Ngân hàng TMCP Sài Gòn Thương Tín)'),
    ('DGC','Duc Giang Chemicals Group (Hóa chất Đức Giang)'),
    ('VRE','Vincom Retail Joint Stock Company (Vincom Retail)'),
    ('SAB','Sabeco (Tổng Công ty CP Bia - Rượu - Nước giải khát Sài Gòn)')
) AS v("TagName", "Note")
WHERE NOT EXISTS (SELECT 1 FROM "Tag" t WHERE t."TagName" = v."TagName");

-- RAISE NOTICE 'Section C done: tags added.';

-- =====================================================================================
-- SECTION C+ — TAG → CATEGORY MAPPINGS
-- All 105+ tags mapped to their correct sectors.
-- Uses subquery lookup so IDs are resolved correctly regardless of SERIAL order.
-- Runs AFTER Section C (bulk tags) so all TagName values are present.
-- =====================================================================================
INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
-- ── Technology / Semiconductors / Software & AI ──────────────────────────────────────
VALUES
((SELECT "TagID" FROM "Tag" WHERE "TagName"='AAPL'),   1),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='NVDA'),   8),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='MSFT'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='GOOGL'),  7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='TSLA'),   1),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='AMZN'),   1),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='META'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='IBM'),    7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='INTC'),   8),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='AMD'),    8),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='QCOM'),   8),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='ORCL'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='CRM'),    7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='ADBE'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SONY'),   1),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SNAP'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='PINS'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SPOT'),  7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='RBLX'),  7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='U'),     7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='PLTR'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SNOW'),  7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='DDOG'),  7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='NET'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='ZM'),    7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='DOCU'),  7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='TEAM'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='NOW'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='WDAY'),  7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='PANW'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='CRWD'),  7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='OKTA'),  7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='MDB'),   7),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='TWLO'),  7)
ON CONFLICT DO NOTHING;

-- ── Healthcare / Biotechnology ───────────────────────────────────────────────────────
INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
VALUES
((SELECT "TagID" FROM "Tag" WHERE "TagName"='PFE'),   2),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='UNH'),   2),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='JNJ'),   2),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='MRNA'),  9),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='LLY'),   2),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='ABBV'),  9)
ON CONFLICT DO NOTHING;

-- ── Finance / Commercial Banking ──────────────────────────────────────────────────────
INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
VALUES
((SELECT "TagID" FROM "Tag" WHERE "TagName"='JPM'),   10),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='PYPL'),  3),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='V'),     3),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='MA'),    3),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='BAC'),  10),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='WFC'),  10),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='GS'),    3),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='MS'),    3),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='C'),    10),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SQ'),    3)
ON CONFLICT DO NOTHING;

-- ── Energy / Green Energy ────────────────────────────────────────────────────────────
INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
VALUES
((SELECT "TagID" FROM "Tag" WHERE "TagName"='XOM'),    4),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='CVX'),   4),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='COP'),   4),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SLB'),   4),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='NEE'),  11),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='DUK'),   4)
ON CONFLICT DO NOTHING;

-- ── Consumer Goods / E-commerce ──────────────────────────────────────────────────────
INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
VALUES
((SELECT "TagID" FROM "Tag" WHERE "TagName"='DIS'),   6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='KO'),    6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='PEP'),   6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='MCD'),   6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SBUX'),  6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='NKE'),   6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='WMT'),   6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='TGT'),   6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='COST'),  6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='HD'),    6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='LOW'),   6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='UBER'),  6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='LYFT'),  6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='ABNB'),  6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='DASH'),  6),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SHOP'), 12),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='ETSY'),  12),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='EBAY'),  12),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='BABA'),  12),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='JD'),   12),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='PDD'),  12),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SE'),   12),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='MELI'), 12)
ON CONFLICT DO NOTHING;

-- ── Cryptocurrencies ───────────────────────────────────────────────────────────────
INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
VALUES
((SELECT "TagID" FROM "Tag" WHERE "TagName"='BTC'),   5),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='ETH'),   5),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='COIN'),  5),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SOL'),    5),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='ADA'),   5),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='DOGE'),  5),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='XRP'),  5),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='BNB'),   5),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='LTC'),   5),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='DOT'),   5),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='LINK'),  5)
ON CONFLICT DO NOTHING;

-- ── Industrials ─────────────────────────────────────────────────────────────────────
INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
SELECT (SELECT "TagID" FROM "Tag" WHERE "TagName"=v.tname), c."CategoryID"
FROM "Category" c
CROSS JOIN (VALUES ('BA'),('GE'),('CAT'),('MMM')) AS v(tname)
WHERE c."CategoryName" = 'Industrials'
ON CONFLICT DO NOTHING;

-- ── Telecommunications ──────────────────────────────────────────────────────────────
INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
SELECT (SELECT "TagID" FROM "Tag" WHERE "TagName"=v.tname), c."CategoryID"
FROM "Category" c
CROSS JOIN (VALUES ('T'),('VZ'),('TMUS'),('CMCSA')) AS v(tname)
WHERE c."CategoryName" = 'Telecommunications'
ON CONFLICT DO NOTHING;

-- ── Media & Streaming ───────────────────────────────────────────────────────────────
INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
SELECT (SELECT "TagID" FROM "Tag" WHERE "TagName"='NFLX'), c."CategoryID"
FROM "Category" c
WHERE c."CategoryName" = 'Media & Streaming'
ON CONFLICT DO NOTHING;

-- ── Automotive ─────────────────────────────────────────────────────────────────────
INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
SELECT (SELECT "TagID" FROM "Tag" WHERE "TagName"=v.tname), c."CategoryID"
FROM "Category" c
CROSS JOIN (VALUES ('F'),('GM'),('TM'),('HMC')) AS v(tname)
WHERE c."CategoryName" = 'Industrials'
ON CONFLICT DO NOTHING;

INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
VALUES
((SELECT "TagID" FROM "Tag" WHERE "TagName"='RIVN'),  1),
((SELECT "TagID" FROM "Tag" WHERE "TagName"='LCID'),  1)
ON CONFLICT DO NOTHING;

-- ── Vietnamese Stock Market Mappings ────────────────────────────────────────────────
INSERT INTO "TagCategoryMap" ("TagID", "CategoryID")
VALUES
((SELECT "TagID" FROM "Tag" WHERE "TagName"='FPT'),    7), -- Software & AI
((SELECT "TagID" FROM "Tag" WHERE "TagName"='VCB'),   10), -- Commercial Banking
((SELECT "TagID" FROM "Tag" WHERE "TagName"='BID'),   10), -- Commercial Banking
((SELECT "TagID" FROM "Tag" WHERE "TagName"='CTG'),   10), -- Commercial Banking
((SELECT "TagID" FROM "Tag" WHERE "TagName"='TCB'),   10), -- Commercial Banking
((SELECT "TagID" FROM "Tag" WHERE "TagName"='MBB'),   10), -- Commercial Banking
((SELECT "TagID" FROM "Tag" WHERE "TagName"='VPB'),   10), -- Commercial Banking
((SELECT "TagID" FROM "Tag" WHERE "TagName"='STB'),   10), -- Commercial Banking
((SELECT "TagID" FROM "Tag" WHERE "TagName"='VNM'),    6), -- Consumer Goods
((SELECT "TagID" FROM "Tag" WHERE "TagName"='MSN'),    6), -- Consumer Goods
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SAB'),    6), -- Consumer Goods
((SELECT "TagID" FROM "Tag" WHERE "TagName"='PNJ'),    6), -- Consumer Goods
((SELECT "TagID" FROM "Tag" WHERE "TagName"='MWG'),   12), -- E-commerce / Retail
((SELECT "TagID" FROM "Tag" WHERE "TagName"='HPG'),    1), -- Materials / Technology
((SELECT "TagID" FROM "Tag" WHERE "TagName"='DGC'),    1), -- Materials
((SELECT "TagID" FROM "Tag" WHERE "TagName"='GAS'),    4), -- Energy
((SELECT "TagID" FROM "Tag" WHERE "TagName"='PLX'),    4), -- Energy
((SELECT "TagID" FROM "Tag" WHERE "TagName"='SSI'),    3), -- Finance
((SELECT "TagID" FROM "Tag" WHERE "TagName"='VND'),    3), -- Finance
((SELECT "TagID" FROM "Tag" WHERE "TagName"='VIC'),    1), -- Real Estate / Tech
((SELECT "TagID" FROM "Tag" WHERE "TagName"='VHM'),    1), -- Real Estate
((SELECT "TagID" FROM "Tag" WHERE "TagName"='VRE'),    6), -- Retail / Real Estate
((SELECT "TagID" FROM "Tag" WHERE "TagName"='VJC'),    6), -- Transportation
((SELECT "TagID" FROM "Tag" WHERE "TagName"='HVN'),    6), -- Transportation
((SELECT "TagID" FROM "Tag" WHERE "TagName"='REE'),    4)  -- Green Energy
ON CONFLICT DO NOTHING;

-- ── Vietnamese Stock Market Featured Articles ──────────────────────────────────────
INSERT INTO "NewsArticle" ("NewsTitle", "Headline", "CreatedDate", "NewsContent", "NewsSource", "CategoryID", "NewsStatus", "CreatedByID", "UpdatedByID", "ModifiedDate", "ConfidenceScore")
VALUES
('[BUY] FPT Automated Analysis',
 'FPT bứt phá doanh thu từ mảng AI và xuất khẩu phần mềm sang thị trường Nhật Bản, Mỹ.',
 NOW() - INTERVAL '1 day',
 '### Executive Summary**: We assign a Strong Buy rating to FPT. The action plan is to accumulate on pullbacks toward the support level.' || E'\n\n' ||
 '(1) Sentiment view: Tâm lý thị trường dành cho FPT cực kỳ tích cực. Dòng tiền khối ngoại và các quỹ đầu tư lớn liên tục mua ròng nhờ triển vọng tăng trưởng doanh thu 20%+ mỗi năm từ thị trường nước ngoài.' || E'\n\n' ||
 '(2) Fundamental view: Doanh thu xuất khẩu phần mềm đạt mốc kỷ lục nhờ mở rộng hợp tác chiến lược về AI với NVIDIA. Biên lợi nhuận gộp duy trì ổn định ở mức 38-40%, dòng tiền từ hoạt động kinh doanh rất mạnh mẽ.' || E'\n\n' ||
 '(3) Key risk warnings: Biến động tỷ giá JPY/VND ảnh hưởng nhẹ tới doanh thu chuyển đổi từ thị trường Nhật Bản và áp lực cạnh tranh thu hút nhân sự công nghệ.',
 'TradingAgents Multi-Agent + gpt-4o',
 7, TRUE, 1, 1, NOW() - INTERVAL '1 day', 92),

('[BUY] HPG Automated Analysis',
 'Hòa Phát đẩy nhanh tiến độ đại dự án Dung Quất 2, sản lượng thép HRC tăng trưởng mạnh.',
 NOW() - INTERVAL '3 days',
 '### Executive Summary**: We assign a Buy rating to HPG. Target price set at 32,000 VND as Dung Quat 2 complex comes online.' || E'\n\n' ||
 '(1) Sentiment view: Giao dịch sôi động với khối lượng lớn. Giới phân tích đánh giá cao năng lực cạnh tranh về chi phí sản xuất thép của HPG so với các đối thủ trong khu vực.' || E'\n\n' ||
 '(2) Fundamental view: Dự án Dung Quất 2 giúp nâng công suất thép thô lên 14 triệu tấn/năm, tối ưu hóa chi phí sản xuất trên mỗi đơn vị sản phẩm. Tỷ lệ nợ/vốn chủ sở hữu ở mức an toàn.' || E'\n\n' ||
 '(3) Key risk warnings: Giá quặng sắt và than cốc đầu vào tăng đột biến cùng rủi ro thuế chống bán phá giá từ các thị trường xuất khẩu.',
 'TradingAgents Multi-Agent + gpt-4o',
 1, TRUE, 2, 2, NOW() - INTERVAL '3 days', 88),

('[HOLD] VCB Automated Analysis',
 'Vietcombank duy trì chất lượng tài sản hàng đầu toàn ngành ngân hàng Việt Nam.',
 NOW() - INTERVAL '4 days',
 '### Executive Summary**: We assign a Hold rating to VCB. Vietcombank remains the premium banking asset in Vietnam with industry-leading LDR and NPL coverage ratio.' || E'\n\n' ||
 '(1) Sentiment view: Cổ phiếu trụ cột giữ nhịp thị trường VN-Index. Nhà đầu tư tổ chức đánh giá cao tính an toàn và uy tín hàng đầu của VCB.' || E'\n\n' ||
 '(2) Fundamental view: Tỷ lệ nợ xấu (NPL) duy trì dưới 1%, tỷ lệ bao phủ nợ xấu vượt 200%, cao nhất hệ thống ngân hàng. NIM ổn định nhờ chi phí vốn thấp và lượng CASA dồi dào.' || E'\n\n' ||
 '(3) Key risk warnings: Tăng trưởng tín dụng toàn ngành chậm lại do cầu hấp thụ vốn của nền kinh tế chưa hồi phục hoàn toàn.',
 'TradingAgents Multi-Agent + gpt-4o',
 10, TRUE, 1, 1, NOW() - INTERVAL '4 days', 85),

('[BUY] VNM Automated Analysis',
 'Vinamilk đẩy mạnh tái cấu trúc thương hiệu và mở rộng thị phần nội địa.',
 NOW() - INTERVAL '6 days',
 '### Executive Summary**: We assign a Buy rating to VNM with high dividend yield appeal.' || E'\n\n' ||
 '(1) Sentiment view: Cổ phiếu thu hút các nhà đầu tư giá trị nhờ cổ tức tiền mặt cao và ổn định hàng năm (30-40%).' || E'\n\n' ||
 '(2) Fundamental view: Giá bột sữa nguyên liệu thế giới giảm giúp cải thiện biên lợi nhuận gộp. Hệ thống nhận diện thương hiệu mới giúp tiếp cận hiệu quả thế hệ người tiêu dùng Gen Z.' || E'\n\n' ||
 '(3) Key risk warnings: Áp lực cạnh tranh từ các thương hiệu sữa ngoại nhập và sữa hạt thế hệ mới.',
 'TradingAgents Multi-Agent + gpt-4o',
 6, TRUE, 3, 3, NOW() - INTERVAL '6 days', 82),

('[HOLD] MWG Automated Analysis',
 'Bách Hóa Xanh đạt điểm hòa vốn, Thế Giới Di Động tối ưu hóa chuỗi cửa hàng.',
 NOW() - INTERVAL '8 days',
 '### Executive Summary**: We assign a Hold rating to MWG as Bach Hoa Xanh transitions into a profitable growth engine.' || E'\n\n' ||
 '(1) Sentiment view: Nhà đầu tư theo dõi sát sao tiến độ phát hành riêng lẻ cổ phần Bách Hóa Xanh cho nhà đầu tư chiến lược.' || E'\n\n' ||
 '(2) Fundamental view: Bách Hóa Xanh đạt doanh thu trung bình/cửa hàng/tháng vượt 1.8 tỷ VNĐ, đóng góp tích cực vào lợi nhuận tập đoàn. Chuỗi ICT đóng bớt các cửa hàng hiệu quả thấp để tập trung tối ưu doanh thu/m2.' || E'\n\n' ||
 '(3) Key risk warnings: Sức mua các mặt hàng không thiết yếu (ICT & Điện máy) phục hồi chậm.',
 'TradingAgents Multi-Agent + gpt-4o',
 12, TRUE, 2, 2, NOW() - INTERVAL '8 days', 80);

-- Map NewsArticles to Vietnam Stock Tags
INSERT INTO "NewsTag" ("NewsArticleID", "TagID") VALUES
((SELECT "NewsArticleID" FROM "NewsArticle" WHERE "NewsTitle"='[BUY] FPT Automated Analysis' LIMIT 1), (SELECT "TagID" FROM "Tag" WHERE "TagName"='FPT')),
((SELECT "NewsArticleID" FROM "NewsArticle" WHERE "NewsTitle"='[BUY] HPG Automated Analysis' LIMIT 1), (SELECT "TagID" FROM "Tag" WHERE "TagName"='HPG')),
((SELECT "NewsArticleID" FROM "NewsArticle" WHERE "NewsTitle"='[HOLD] VCB Automated Analysis' LIMIT 1), (SELECT "TagID" FROM "Tag" WHERE "TagName"='VCB')),
((SELECT "NewsArticleID" FROM "NewsArticle" WHERE "NewsTitle"='[BUY] VNM Automated Analysis' LIMIT 1), (SELECT "TagID" FROM "Tag" WHERE "TagName"='VNM')),
((SELECT "NewsArticleID" FROM "NewsArticle" WHERE "NewsTitle"='[HOLD] MWG Automated Analysis' LIMIT 1), (SELECT "TagID" FROM "Tag" WHERE "TagName"='MWG'))
ON CONFLICT DO NOTHING;

-- RAISE NOTICE 'Section C+ done: TagCategoryMap populated.';

-- =====================================================================================
-- SECTION D — BULK NEWS ARTICLES + NEWS TAGS
-- Generates 5000 articles, ~5% CreatedByID=NULL, ~15% Inactive
-- =====================================================================================
DO $$
DECLARE
    num_articles INT := 5000;
    i INT;
    v_category_id INT;
    v_tag_id INT;
    v_tag_name VARCHAR(50);
    v_staff_id INT;
    v_creator_id INT;
    v_decision VARCHAR(10);
    v_rand_val INT;
    v_sentiment TEXT;
    v_fundamental TEXT;
    v_risk TEXT;
    v_created_date TIMESTAMP;
    v_is_active BOOLEAN;
    v_news_article_id INT;
    v_sentiment_variants TEXT[] := ARRAY[
        'Sentiment view: Market sentiment is broadly positive, driven by strong quarterly performance and favorable analyst commentary.',
        'Sentiment view: Sentiment has turned cautious amid mixed signals from recent earnings calls and shifting institutional positioning.',
        'Sentiment view: Overall sentiment remains neutral, with investors awaiting further clarity on near-term catalysts.',
        'Sentiment view: Negative sentiment prevails following disappointing guidance and increased short interest.',
        'Sentiment view: Sentiment is improving steadily as recent news flow skews constructive across financial media.'
    ];
    v_fundamental_variants TEXT[] := ARRAY[
        'Fundamental view: Revenue growth remains healthy with expanding margins and a solid balance sheet.',
        'Fundamental view: Fundamentals show some pressure from rising input costs and competitive dynamics.',
        'Fundamental view: Core business metrics are stable, though growth has decelerated from prior quarters.',
        'Fundamental view: Strong free cash flow generation supports continued reinvestment and shareholder returns.',
        'Fundamental view: Balance sheet leverage has increased, warranting a closer look at debt servicing capacity.'
    ];
    v_risk_variants TEXT[] := ARRAY[
        'Key risk warnings: Macroeconomic headwinds, including interest rate uncertainty, could weigh on valuation multiples.',
        'Key risk warnings: Regulatory scrutiny in key markets poses a potential overhang on future growth.',
        'Key risk warnings: Sector-wide competitive pressure may compress margins further in coming quarters.',
        'Key risk warnings: Currency fluctuations and supply chain constraints remain notable watch items.',
        'Key risk warnings: Broader market volatility could disproportionately affect near-term price action.'
    ];
BEGIN
    FOR i IN 1..num_articles LOOP
        -- Random Category
        SELECT c."CategoryID" INTO v_category_id
        FROM "Category" c
        ORDER BY RANDOM()
        LIMIT 1;

        -- Random Tag
        SELECT t."TagID", t."TagName" INTO v_tag_id, v_tag_name
        FROM "Tag" t
        ORDER BY RANDOM()
        LIMIT 1;

        -- Random Staff creator
        SELECT a."AccountID" INTO v_staff_id
        FROM "SystemAccount" a
        WHERE a."AccountRole" = 1
        ORDER BY RANDOM()
        LIMIT 1;

        -- ~5% CreatedByID = NULL (deleted user simulation)
        IF RANDOM() < 0.05 THEN
            v_creator_id := NULL;
        ELSE
            v_creator_id := v_staff_id;
        END IF;

        -- Decision: 40% BUY, 30% SELL, 30% HOLD
        v_rand_val := FLOOR(RANDOM() * 100)::INT;
        IF v_rand_val < 40 THEN
            v_decision := 'BUY';
        ELSIF v_rand_val < 70 THEN
            v_decision := 'SELL';
        ELSE
            v_decision := 'HOLD';
        END IF;

        -- Random date within last 730 days
        v_created_date := NOW() - (RANDOM() * 730 || ' days')::INTERVAL;

        -- ~85% Active, ~15% Inactive
        v_is_active := RANDOM() < 0.85;

        -- Random content variants
        v_sentiment := v_sentiment_variants[1 + (RANDOM() * 4)::INT];
        v_fundamental := v_fundamental_variants[1 + (RANDOM() * 4)::INT];
        v_risk := v_risk_variants[1 + (RANDOM() * 4)::INT];

        INSERT INTO "NewsArticle"
            ("NewsTitle", "Headline", "CreatedDate", "NewsContent", "NewsSource",
             "CategoryID", "NewsStatus", "CreatedByID", "UpdatedByID", "ModifiedDate", "ConfidenceScore")
        VALUES (
            '[' || v_decision || '] ' || v_tag_name || ' Automated Analysis',
            v_decision || ' signal generated for ' || v_tag_name || ' based on synthesized sentiment and fundamental review.',
            v_created_date,
            '(1) ' || v_sentiment || E'\n\n' || '(2) ' || v_fundamental || E'\n\n' || '(3) ' || v_risk,
            'NewsAPI.org + gpt-4o (bulk seed)',
            v_category_id,
            v_is_active,
            v_creator_id,
            CASE WHEN v_creator_id IS NOT NULL AND RANDOM() < 0.20 THEN v_creator_id ELSE NULL END,
            CASE WHEN v_creator_id IS NOT NULL AND RANDOM() < 0.20 THEN v_created_date + INTERVAL '30 minutes' ELSE NULL END,
            70 + FLOOR(RANDOM() * 31)::INTEGER
        );

        v_news_article_id := LASTVAL();

        INSERT INTO "NewsTag" ("NewsArticleID", "TagID")
        VALUES (v_news_article_id, v_tag_id);

        IF i % 500 = 0 THEN
            RAISE NOTICE 'Generated % / % NewsArticles...', i, num_articles;
        END IF;
    END LOOP;
END $$;

-- RAISE NOTICE 'Section D done: NewsArticle + NewsTag added.';

-- =====================================================================================
-- SECTION E — VERIFICATION QUERIES
-- =====================================================================================
SELECT 'SystemAccount' AS table_name, COUNT(*) AS row_count FROM "SystemAccount"
UNION ALL SELECT 'Category', COUNT(*) FROM "Category"
UNION ALL SELECT 'Tag', COUNT(*) FROM "Tag"
UNION ALL SELECT 'NewsArticle', COUNT(*) FROM "NewsArticle"
UNION ALL SELECT 'NewsTag', COUNT(*) FROM "NewsTag"
UNION ALL SELECT 'SavedReport', COUNT(*) FROM "SavedReport"
UNION ALL SELECT 'TagCategoryMap', COUNT(*) FROM "TagCategoryMap";

SELECT "NewsStatus", COUNT(*) AS total FROM "NewsArticle" GROUP BY "NewsStatus";
SELECT SUBSTRING("NewsTitle" FROM 1 FOR 6) AS decision_prefix, COUNT(*) AS total FROM "NewsArticle" GROUP BY 1;
SELECT MIN("CreatedDate") AS earliest_date, MAX("CreatedDate") AS latest_date FROM "NewsArticle";

SELECT a."AccountName", COUNT(n."NewsArticleID") AS report_count
FROM "SystemAccount" a
LEFT JOIN "NewsArticle" n ON n."CreatedByID" = a."AccountID"
WHERE a."AccountRole" = 1
GROUP BY a."AccountName"
ORDER BY report_count DESC
LIMIT 10;

-- TagCategoryMap coverage by sector
SELECT c."CategoryName", COUNT(tcm."TagID") AS mapped_tags
FROM "TagCategoryMap" tcm
JOIN "Category" c ON c."CategoryID" = tcm."CategoryID"
GROUP BY c."CategoryName"
ORDER BY mapped_tags DESC;

-- Tags without any sector mapping (should be 0)
SELECT t."TagName" AS unmapped_tag FROM "Tag" t
WHERE NOT EXISTS (
    SELECT 1 FROM "TagCategoryMap" tcm WHERE tcm."TagID" = t."TagID"
);

-- Test accounts present
SELECT "AccountEmail", "AccountRole" FROM "SystemAccount"
WHERE "AccountEmail" IN ('staff@FUNewsTradingSystem.org','lecturer@FUNewsTradingSystem.org');

-- =====================================================================================
-- SECTION F — RESET POSTGRESQL SEQUENCES TO PREVENT DUPLICATE KEY DB_ERROR
-- =====================================================================================
SELECT setval(pg_get_serial_sequence('"NewsArticle"', 'NewsArticleID'), COALESCE((SELECT MAX("NewsArticleID") FROM "NewsArticle"), 1));
SELECT setval(pg_get_serial_sequence('"Tag"', 'TagID'), COALESCE((SELECT MAX("TagID") FROM "Tag"), 1));
SELECT setval(pg_get_serial_sequence('"Category"', 'CategoryID'), COALESCE((SELECT MAX("CategoryID") FROM "Category"), 1));
SELECT setval(pg_get_serial_sequence('"SystemAccount"', 'AccountID'), COALESCE((SELECT MAX("AccountID") FROM "SystemAccount"), 1));
SELECT setval(pg_get_serial_sequence('"SavedReport"', 'SavedReportID'), COALESCE((SELECT MAX("SavedReportID") FROM "SavedReport"), 1));
SELECT setval(pg_get_serial_sequence('"TagCategoryMap"', 'TagCategoryMapID'), COALESCE((SELECT MAX("TagCategoryMapID") FROM "TagCategoryMap"), 1));

-- RAISE NOTICE '=== BULK SEED & SEQUENCE SYNC COMPLETE ===';
