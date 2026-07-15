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

RAISE NOTICE 'FUNewsTradingSystem Database Schema and Seed Data deployed successfully!';

-- =====================================================================================
-- BULK TEST DATA EXPANSION
-- Purpose: Test pagination, admin statistics, staff report history, and filtering
-- =====================================================================================

RAISE NOTICE '=== BULK SEED: Starting...';

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


RAISE NOTICE 'Section A done: accounts added.';

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

RAISE NOTICE 'Section B done: categories added.';

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
) AS v("TagName", "Note")
WHERE NOT EXISTS (SELECT 1 FROM "Tag" t WHERE t."TagName" = v."TagName");

RAISE NOTICE 'Section C done: tags added.';

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

RAISE NOTICE 'Section D done: NewsArticle + NewsTag added.';

-- =====================================================================================
-- SECTION E — VERIFICATION QUERIES
-- =====================================================================================
SELECT 'SystemAccount' AS table_name, COUNT(*) AS row_count FROM "SystemAccount"
UNION ALL SELECT 'Category', COUNT(*) FROM "Category"
UNION ALL SELECT 'Tag', COUNT(*) FROM "Tag"
UNION ALL SELECT 'NewsArticle', COUNT(*) FROM "NewsArticle"
UNION ALL SELECT 'NewsTag', COUNT(*) FROM "NewsTag";

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

RAISE NOTICE '=== BULK SEED COMPLETE ===';
