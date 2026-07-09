# Git Collaboration Guide (FUNews Management System)

This document specifies how 4 members (P1, P2, P3, P4) collaborate on Git. The core goal is to **minimize the process**, **avoid Merge Conflicts caused by Entity Framework (EF)**, and **keep the project always running**.

---

## 1. Branching Strategy

With an 8-week project for 4 people, applying GitFlow (with a `dev` branch in the middle) is **unnecessary and redundant**. We will use **GitHub Flow**.

- **There is only one fixed branch: `main`.**
- **The Supreme Rule:** Code on `main` **MUST ALWAYS BE RUNNING**. Never push code that has a corrupted migration to `main`.
- **Create a short-term branch (Feature Branch):** Each time you work on a new feature, create a branch from `main`, finish it, merge it straight into `main`, and **delete that branch**.

---

## 2. Naming Conventions

The branch name must clearly show who is doing what. Syntax: `[role]/[feature-name]` (lowercase, no accents, use hyphens).

**Examples:**
- `p1/authentication-system` (P1 does authentication)
- `p2/user-profile-system` (P2 does user profile)
- `p3/news-display-system` (P3 does news display)
- `p4/news-management-system` (P4 does news management)

---

## 3. Daily Workflow

Each time you start working on a new task, follow these 5 steps exactly:

1. **Update to the latest code:**
   ```bash
   git checkout main
   git pull origin main
   ```
2. **Create a new branch for your task:**
   ```bash
   git checkout -b p1/feature-name
   ```
3. **Work and Commit:**
   - Commit small, meaningful changes. Don't lump 3 days of code into one commit.
   - Write clear commit messages.
   ```bash
   git add .
   git commit -m "fix/feat: ABC feature"
   ```
4. **Push code to GitHub and create a Pull Request (PR):**
   ```bash
   git push origin p1/feature-name
   ```
   - Go to GitHub to create a Pull Request targeting the `main` branch.
   - Ask another team member (or review it yourself if you've tested it thoroughly) to click **Merge pull request**.
5. **Clean up:**
   - Delete the branch you just created on GitHub.
   - Go back to your personal computer, switch to `main`, delete the local branch, and pull the new code:
   ```bash
   git checkout main
   git pull origin main
   git branch -d p1/feature-name
   ```

---

## 4. ⚠️ Entity Framework (EF) PROJECT RULES ⚠️

EF generates many complex migration files (especially SQL scripts). If not careful, the team will fall into "Merge Conflict Hell" (hell of code conflicts). Here are 3 golden rules:

### Rule 1: Never edit the same migration file with another person at the same time.
- EF migration files (`.cs` in `Migrations` folder) are very fragile and can be corrupted if two people edit and push them at the same time. 
- **Solution:** Divide tasks clearly. For example, if P3 is editing the migration file for the news system, P1 and P2 must **absolutely not open and save that migration file**. 
- Instead, P1 and P2 should work on **model classes** (e.g., `News.cs`, `Category.cs`). When P1 pushes the model class, P3's migration file will be updated automatically without any Conflict.

### Rule 2: How to deal with EF merge conflicts
- If a conflict occurs in C# code (`.cs`), calmly open VS Code and resolve it as usual.
- But if the conflict occurs in `.cs` (Code) files: **NEVER try to fix it manually** because 99% it will corrupt the file.
- **The Solution:** Discuss whose code is more important, choose to Accept Incoming or Accept Current (keep one side completely), the other person should open Visual Studio or Visual Studio Code and fix it.

---

## 5. Communication Summary
- Before creating a dynamic branch into someone else's system, chat in the group: *"I'm editing the EF migration files, everyone please don't touch that EF migration file."* 
- P1, when pushing EF migration files, remember to remind P2, P3, P4 to pull the code.
