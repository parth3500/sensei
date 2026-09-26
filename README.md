# 🥋 Sensei — AI-Powered GATE 2027 Preparation Engine

A modular, component-assembled full-stack study assistant for GATE Computer Science & Information Technology, powered by **C# (.NET 8 Web API)** and **Next.js 14 (React 18 + Tailwind CSS)**.

Designed for high-retention learning with active recall, Google Drive note synchronization, video lecture progress tracking, and collaborator-friendly pluggable modules.

---

## 🏗️ Architecture: Component Assembly Model (CBSE)

The system is built on decoupled, independently testable components connected via clean dependency injection and API contracts:

```
[ Next.js 14 PWA Mobile Client (Port 3000) ]
  ├── TodayView             ── Daily task execution & focus
  ├── PracticeView          ── SM-2 Spaced Repetition & Ebbinghaus Recall
  ├── LecturesView          ── Video progress, timestamps & notes
  ├── DriveStorageView      ── Google Drive folder ingestion & DB backup
  ├── ProgressView          ── Advanced analytics, retention rate, subject mastery
  ├── AiTutorView           ── Hinglish GATE AI Tutor
  └── ModulesHubView        ── Pluggable collaborator extension registry
         │ (HTTP / JSON Proxy)
         ▼
[ C# .NET 8 Web API (Port 5000) ]
  ├── AuthComponent                ── SHA-256 + constant-time passphrase auth
  ├── SqliteDatabaseComponent      ── Zero-NuGet native P/Invoke to libsqlite3.so
  ├── SpacedRepetitionComponent    ── SM-2 algorithmic scheduling
  ├── VideoLectureComponent        ── YouTube ID & timestamp tracking
  ├── GoogleDriveStorageComponent  ── Ingestion, auto-summary & question generator
  ├── AnalyticsComponent           ── Ebbinghaus decay curve & syllabus mastery
  ├── AiTutorComponent             ── Persona & question generation
  └── ModuleManagerComponent       ── Pluggable component registry
```

---

## 🚀 Key Features

### 1. 📂 Google Drive Storage & Automatic Ingestion
- Live sync folder: `GATE_Sensei_Notes/` (located under `backend/Drive_Storage/`).
- Drop any notes, lecture transcripts, or cheat sheets into the folder or ingest via UI:
  - Auto-extracts rapid revision bullet points.
  - Automatically synthesizes practice questions and seeds them directly into the question bank!
  - 1-click database snapshot backups to `Drive_Storage/Backups/`.

### 2. 🧠 Active Recall & Forgetting Curve (Ebbinghaus Decay)
- Tracks retention probability using $R = e^{-t / S}$ where $t$ is days elapsed and $S$ is memory stability.
- **At-Risk Recall Mode**: Instantly flags concepts slipping below retention thresholds so you review before forgetting accelerates.

### 3. 📹 Video Lecture Progress Tracker
- Paste YouTube or web video lecture links.
- Tracks exact playback timestamps (e.g. `24:10 / 52:00`), completion status, and study notes.
- Automatically folds lecture time into your study analytics.

### 4. 📊 Advanced Analytics & Mastery Dashboard
- **Memory Retention Index**: Real-time gauge of memory stability across all studied concepts.
- **Subject Syllabus Mastery**: Progress bars across all 8 GATE core subjects (Algorithms, OS, Networks, DBMS, TOC, etc.).
- **Time Allocation Breakdown**: Visual distribution between Video Lectures, Spaced Practice, and Notes Revision.
- **Activity Consistency Heatmap**: 90-day GitHub-style study streak visualization.

### 5. 🧩 Pluggable Modules Hub (Collaborator-Ready)
- Your friend is creating new modules? They can implement clean C# controllers and Next.js view components and register them via `POST /api/modules` or the Modules Hub UI without breaking the core system.

### 6. 📱 Mobile Progressive Web App (PWA)
- Optimized for mobile screens with touch-friendly navigation, standalone display mode, dark UI, and service worker caching.
- On your phone: Open `http://<your-host-ip>:3000` in Chrome/Safari, tap **"Add to Home Screen"** or **"Install App"** to use it as a native mobile app!

---

## ⚡ Quick Start (Local)

### 1. Start Both Backend and Frontend:
```bash
./start.sh
```
- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:5000`
- Passphrase: `your-secure-passphrase-here` (or customize via `APP_PASSPHRASE` in `.env`)

---

## 🐳 Docker Deployment

To build and run both the C# backend and Next.js frontend with Docker:

```bash
docker compose up --build -d
```

- Accessible on `http://localhost:3000`.
- SQLite database and Google Drive sync folders are safely persisted in Docker volumes (`sensei-db` and `sensei-drive`).

---

## 📡 API Endpoints Summary

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/login` | Authenticate with passphrase |
| `GET` | `/api/auth/me` | Check session state |
| `GET` | `/api/tasks` | Get daily tasks (filter by date/status) |
| `POST` | `/api/tasks` | Create study task |
| `GET` | `/api/questions` | Get questions for practice |
| `POST` | `/api/questions/{id}/attempts` | Submit answer & trigger SM-2 update |
| `GET` | `/api/lectures` | List video lecture trackers |
| `PATCH`| `/api/lectures/{id}/progress` | Update lecture timestamp & notes |
| `GET` | `/api/drive/status` | Check Drive storage & folder status |
| `POST` | `/api/drive/ingest` | Ingest note, extract summary & questions |
| `POST` | `/api/drive/backup` | Create snapshot DB backup |
| `GET` | `/api/stats/advanced` | Retention index, mastery & time spent |
| `GET` | `/api/stats/forgetting-risk` | High-risk fading concepts |
| `GET` | `/api/modules` | Pluggable module registry |
| `POST` | `/api/ai/chat` | Chat with Sensei AI tutor |

---

## 🛡️ License & Author
Built for GATE 2027 Aspirants. Component Assembly Model architecture.
