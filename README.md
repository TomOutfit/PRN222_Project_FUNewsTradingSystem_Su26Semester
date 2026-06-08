# FUNewsTradingSystem

FUNewsTradingSystem is an automated, AI-powered trading analysis web application built with ASP.NET Core MVC. The system fetches real-time market news via NewsAPI and utilizes OpenAI LLMs to perform sentiment and fundamental analysis, ultimately providing actionable portfolio management decisions (BUY/SELL/HOLD).

**Live Application:** [https://prn222-project-funewstradingsystem.onrender.com/](https://prn222-project-funewstradingsystem.onrender.com/)

---

## Architecture

The project strictly follows a multi-tier architectural pattern to enforce separation of concerns:
* **MVC (Presentation Layer):** Handles user interactions, views, controllers, and acts as the entry point.
* **BusinessLayer:** Contains service implementations, external API integrations (NewsAPI, OpenAI), and the core AI Trading Pipeline logic.
* **DataAccessLayer:** Manages database contexts, entity models, and data access repositories using Entity Framework Core.

## Tech Stack

* **Framework:** .NET 10 (ASP.NET Core MVC)
* **ORM:** Entity Framework Core
* **Database:** Microsoft SQL Server (LocalDB for development, Azure/Somee for production)
* **Frontend:** HTML5, CSS3, Bootstrap 5.3 (Includes native Light/Dark Mode)
* **External APIs:** NewsAPI.org, OpenAI API (gpt-4o)

## Features & Roles

The system uses Claims-based Authentication with Role-based Authorization:

### 1. Admin (Role 3)
* Full Account Management (CRUD operations, reset passwords).
* Statistical Reporting (Filter generated reports by date ranges).

### 2. Staff (Role 1)
* Run Analysis: Trigger the AI pipeline for specific sectors and tickers.
* Category and Tag Management.
* Profile Management and personal Report History.

### 3. Lecturer / Guest (Role 2)
* Public Trading Report Viewer.
* Read-only access to published AI market analysis.

---

## Getting Started

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/download)
* SQL Server Express LocalDB (or any SQL Server instance)
* Visual Studio 2022 or Visual Studio Code

### Local Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-username/FUNewsTradingSystem.git
   cd FUNewsTradingSystem
   ```

2. **Configure Application Settings**
   Copy the `appsettings.json.example` file and rename it to `appsettings.json` inside the `FUNewsTradingSystem/MVC` directory.
   Provide your actual `NewsApi` and `OpenAI` API keys.

3. **Apply Database Migrations**
   The application uses Entity Framework Core for data management. Run the following command from the root directory to create the database:
   ```bash
   dotnet ef database update --project FUNewsTradingSystem/DataAccessLayer --startup-project FUNewsTradingSystem/MVC
   ```
   *(Note: The `Program.cs` is configured to automatically apply pending migrations on startup, so this step can often be skipped).*

4. **Run the Application**
   ```bash
   dotnet run --project FUNewsTradingSystem/MVC
   ```
   The application will start, usually at `http://localhost:5000` or `https://localhost:5001`.

### Default Test Credentials
If the seed data was applied successfully, you can log in using:
* **Admin:** `admin@FUNewsTradingSystem.org` | Password: `@@abc123@@`

---

## Deployment

The application is fully containerized and ready for cloud deployment. A multi-stage `Dockerfile` is included at the root of the repository, optimizing the build process for the 3-tier architecture. 

For continuous deployment platforms like Render or Railway:
1. Select Docker as the runtime environment.
2. Provide your cloud database connection string via the `ConnectionStrings__DefaultConnection` environment variable.
3. Provide API keys via `NewsApi__ApiKey` and `OpenAI__ApiKey`.

## Known Limitations

* **NewsAPI Restrictions:** Free tier of NewsAPI may restrict queries to articles within the last 30 days and block requests originating from certain cloud providers.
* **LLM Variability:** Automated analysis decisions depend entirely on the context provided by the fetched headlines and the specific AI model's generation logic. Outputs should not be construed as actual financial advice.
