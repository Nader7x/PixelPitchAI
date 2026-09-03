# ⚽ PixelPitchAI Frontend Dashboard

A modern, high-performance web client built with **Next.js 16 (React 19, Turbopack, App Router)**. It provides real-time visualizations, team management dashboards, and a 3D match visualizer.

---

## ✨ Features

* **Real-time Match Simulation Streaming**: Renders play-by-play match commentary and live statistics via Server-Sent Events (SSE) connecting to `/api/matches/{id}/events/stream` using the native browser `EventSource` (`MatchStreamService.ts`).
* **3D Pitch Visualizer**: Integrates **Three.js** and **React Three Fiber (R3F)** to render player positions and ball movements in 3D.
* **Multi-Language Support**: Complete internationalization (`next-intl`) supporting English, Spanish, and French.
* **Modern Theming**: Supports dark/light mode with system preference auto-detection.
* **Responsive & PWA Ready**: Optimized for mobile and desktop screens, with optional Progressive Web App features.

---

## 🛠️ Technology Stack

* **Framework**: Next.js 16 (React 19, Turbopack, App Router)
* **UI & Styling**: Tailwind CSS v4, DaisyUI components
* **Graphics**: Three.js, React Three Fiber, Three-stdlib
* **Real-time Match Engine**: Server-Sent Events (SSE) via native browser `EventSource` (`MatchStreamService.ts`) for `/api/matches/{id}/events/stream`
* **Translation**: `next-intl`
* **Package Manager**: `pnpm`

---

## ⚙️ Quick Start

### 1. Install Dependencies
Ensure you have `pnpm` installed, then run:
```bash
pnpm install
```

### 2. Configure Environment Variables
Create `.env.local` in the `frontend` root:
```env
NEXT_PUBLIC_API_URL=http://localhost:5025
```
Key configuration settings:
* `NEXT_PUBLIC_API_URL`: Gateway URL of the core .NET API (`http://localhost:5025` in bare-metal dev, `https://${DOMAIN:-localhost}/api` in Docker/production).

### 3. Start Development Server
```bash
pnpm dev
# Access http://localhost:3000 in your browser
```

---

## 🐳 Docker Deployment

The frontend includes a multi-stage Docker build config and script utilities:

```bash
# Start development container
pnpm run docker:dev

# Start production container with Nginx server
pnpm run docker:prod
```

For production hosting and deployment configuration, see the [Monorepo Guide](../README.md) and [Cloud Deployment Guides](../deploy/).

---

## 📂 Folders & Structure

* [app/](./app) — Next.js App Router containing views, layouts, and API page endpoints.
* [components/](./components) — Reusable UI modules, including standard forms and the 3D stadium scene (`Scene3D.tsx`).
* [messages/](./messages) — Localized translation dictionaries for each supported language.
* [Services/](./Services) — API client layer managing Server-Sent Events match streaming (`MatchStreamService.ts`), HTTP fetch clients, and alert notifications.

