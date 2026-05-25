# Git Collaboration Guide (Galaxy Defender)

This document specifies how 4 members (P1, P2, P3, P4) collaborate on Git. The core goal is to **minimize the process**, **avoid Merge Conflicts caused by Unity**, and **keep the project always running**.

---

## 1. Branching Strategy

With an 8-week project for 4 people, applying GitFlow (with a `dev` branch in the middle) is **unnecessary and redundant**. We will use **GitHub Flow**.

- **There is only one fixed branch: `main`.**
- **The Supreme Rule:** Code on `main` **MUST ALWAYS BE RUNNING**. Never push code that is broken or has a corrupted scene to `main`.
- **Create a short-term branch (Feature Branch):** Each time you work on a new feature, create a branch from `main`, finish it, merge it straight into `main`, and **delete that branch**.

---

## 2. Naming Conventions

The branch name must clearly show who is doing what. Syntax: `[role]/[feature-name]` (lowercase, no accents, use hyphens).

**Examples:**
- `p1/player-movement` (P1 does movement)
- `p2/wave-manager-setup` (P2 does wave system)
- `p3/level1-tilemap` (P3 draws map)
- `p4/import-audio-sfx` (P4 pushes assets)

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

## 4. ⚠️ UNITY PROJECT RULES ⚠️

Unity generates many complex system files (especially YAML). If not careful, the team will fall into "Merge Conflict Hell" (hell of code conflicts). Here are 3 golden rules:

### Rule 1: Never edit the same scene with another person at the same time.
- Unity Scene files (`.unity`) are very fragile and can be corrupted if two people edit and push them at the same time. 
- **Solution:** Divide tasks clearly. For example, if P3 is drawing the map in `Level1.unity`, P1 and P2 must **absolutely not open and save that Scene file**. 
- Instead, P1 and P2 should work on **Prefabs** (e.g., open `Player.prefab` to code/attach scripts). When P1 pushes the Prefab, P3's Scene will be updated automatically without any Conflict.

### Rule 2: Always remember to commit `.meta` files
- Every file in the `Assets` folder (whether it's an image, audio, or script) is automatically created by Unity along with a `.meta` file containing a GUID.
- **Never ignore or forget to commit `.meta` files.** If you delete or move a file, you must remember to delete or move the corresponding `.meta` file.
- If you push an image file without pushing its `.meta` file, P3's computer will generate a new `.meta` code $\rightarrow$ Result: The image will lose its link (Pink Missing Texture) in all Prefabs.

### Rule 3: How to deal with Unity merge conflicts
- If a conflict occurs in C# code (`.cs`), calmly open VS Code and resolve it as usual.
- But if the conflict occurs in `.unity` (Scene) or `.prefab` files: **NEVER try to fix it manually (by reading YAML)** because 99% it will corrupt the file.
- **The Solution:** Discuss whose code is more important, choose to Accept Incoming or Accept Current (keep one side completely), the other person should be patient enough to open Unity and drag & drop the assets back for a few minutes. This is 10 times faster and safer than trying to fix YAML.

---

## 5. Communication Summary
- Before creating a dynamic branch into someone else's system, chat in the group: *"I'm editing the Boss Prefab, everyone please don't touch that Prefab."*
- P4, when pushing image and audio files, remember to remind P1, P2, P3 to pull the code. Avoid the situation where P3 is drawing UI but missing images.
