# Sensei — Deployment & Architecture Memory

This document records the production setup, deployment URLs, APK build, and cost optimizations configured for Sensei.

---

## 1. Cloud Infrastructure (Railway)

- **Project ID**: `b559f361-602d-493a-980a-79b36d766393`
- **Environment**: `production` (`9fb550a9-0824-48ca-be26-ed9d6799b960`)
- **GitHub Repository**: [`parth3500/sensei`](https://github.com/parth3500/sensei)

### Services

| Service | Root Dir | Dockerfile | Public URL | Key Variables |
|---|---|---|---|---|
| **`backend`** | `/backend` | `Dockerfile` | `https://backend-production-4b48d.up.railway.app` | `APP_PASSPHRASE`, `TZ=Asia/Kolkata` |
| **`frontend`** | `/frontend` | `Dockerfile` | `https://frontend-production-6936.up.railway.app` | `BACKEND_URL`, `NODE_ENV=production` |

### Storage & Persistence
- **Persistent Volume**: `backend-volume` mounted to `/app/Data`.
- Stores SQLite database (`sensei.db`) with WAL mode enabled.
- Safe across container restarts and scale-to-zero events.

### Dynamic Reverse Proxy Routing
- Next.js dynamic App Router route handler at `frontend/app/api/[[...path]]/route.ts`.
- Evaluates `process.env.BACKEND_URL` at runtime.
- Proxies all client `/api/*` calls to the Railway backend with cookies and headers preserved.

---

## 2. Cost & Compute Optimizations (Scale-to-Zero)

Both Railway services have **Auto-Sleep (`sleepApplication: true`)** enabled:
- **Idle Behavior**: When the user is inactive, Railway automatically puts containers to sleep.
- **Compute Usage**: 0 vCPU, 0 MB RAM ($0.00/hour during idle periods).
- **Wake-On-Demand**: Incoming requests wake containers in ~1–2 seconds.
- **Replicas**: Strictly locked to 1 replica each (prevents unexpected scaling).

---

## 3. Android APK & PWA

- **APK File**: Located at root [`Sensei.apk`](../Sensei.apk) (`972 KB`).
- **Package Name**: `app.railway.sensei`
- **TWA Verification**: `frontend/public/.well-known/assetlinks.json` configured for full-screen standalone mode without browser chrome.
- **Rebuild Mechanism**: Headless PWABuilder CloudAPK API using the live Railway HTTPS endpoint.

---

## 4. Google Drive & Note Ingestion

- **UI Ingestion**: Bottom navigation -> **Drive** tab. Ingests `.txt`/`.md` notes, generates 2-sentence summary, 4–5 bullet points, and 2 active GATE practice questions.
- **Local Directory**: `backend/Drive_Storage/` (persists raw notes and database snapshots).
- **Snapshot Backups**: Triggered via `POST /api/drive/backup` or UI button to `Drive_Storage/Backups/`.
