# ⚽ Football Simulation Frontend Dashboard

A modern, high-performance web client built with **Next.js 16 (React 19)**. It provides real-time visualizations, team management dashboards, and a 3D match visualizer.

---

## ✨ Features

* **Real-time Match Visualization**: Renders match commentary and events in real-time via SignalR connections.
* **3D Pitch Visualizer**: Integrates **Three.js** and **React Three Fiber (R3F)** to render player positions and ball movements in 3D.
* **Multi-Language Support**: Complete internationalization (next-intl) supporting English, Spanish, and French.
* **Modern Theming**: Supports dark/light mode with system preference auto-detection.
* **Responsive & PWA Ready**: Optimized for mobile and desktop screens, with optional Progressive Web App features.

---

## 🛠️ Technology Stack

* **Framework**: Next.js 16 (App Router)
* **UI & Styling**: Tailwind CSS v4, DaisyUI components
* **Graphics**: Three.js, React Three Fiber, Three-stdlib
* **Real-time Engine**: SignalR WebSockets
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
Copy `.env.example` to `.env.local` and customize the variables:
```bash
cp .env.example .env.local
```
Key configuration settings:
* `NEXT_PUBLIC_API_URL`: Gateway URL of the core .NET API.
* `NEXT_PUBLIC_SIGNALR_URL`: Hub endpoint for SignalR broadcasts.
* `NEXT_PUBLIC_ENABLE_3D`: Enables/disables the 3D visualizer module.

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

For production hosting and automated SSL certificate renewals, see the [Docker Deployment Guide](./DOCKER_DEPLOYMENT_GUIDE.md) and [HTTPS Hosting Guide](./HTTPS_HOSTING_GUIDE.md).

---

## 📂 Folders & Structure

* [app/](file:///d:/programming/GitHub/Footex/frontend/app) — Next.js App Router containing views, layouts, and API page endpoints.
* [components/](file:///d:/programming/GitHub/Footex/frontend/components) — Reusable UI modules, including standard forms and the 3D stadium scene (`Scene3D.tsx`).
* [messages/](file:///d:/programming/GitHub/Footex/frontend/messages) — Localized translation dictionaries for each supported language.
* [Services/](file:///d:/programming/GitHub/Footex/frontend/Services) — API client layer managing SignalR hubs and fetch configurations.
