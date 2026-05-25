# ReWashPlus — Car Wash Management Platform

🔗 **Live Demo:** [https://t-b-o.github.io/ReWashPlus_DemoApp_2026/](https://t-b-o.github.io/ReWashPlus_DemoApp_2026/)

---

## Overview

ReWashPlus is a Progressive Web Application (PWA) built to digitise and streamline the daily operations of professional car wash businesses. The platform replaces manual, paper-based workflows with a fast, modern, offline-capable solution that works reliably in environments with intermittent internet connectivity.

This repository contains the **frontend demo application** — a Blazor WebAssembly PWA that demonstrates the full customer-facing and operational experience, including booking management, payment tracking, staff scheduling, and administrative reporting.

---

## Key Features

- **Offline-First Architecture** — fully functional without an internet connection; data syncs automatically when connectivity is restored
- **Progressive Web App (PWA)** — installable on Android, iOS, and desktop; runs like a native app with no app store required
- **Booking & Job Management** — create, track, and complete wash jobs with real-time status updates
- **Customer & Vehicle Records** — maintain detailed customer profiles and vehicle histories
- **Payments** — record and track payments across multiple methods (cash, card, EFT)
- **Admin Dashboard** — branch-level reporting, staff management, and service configuration
- **Multi-Tenant Ready** — architected to support multiple business branches under a single platform

---

## Technology Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor WebAssembly (.NET 8) |
| PWA & Offline | Service Worker + IndexedDB (Blazored.LocalStorage) |
| Styling | Tailwind CSS |
| Hosting (Demo) | GitHub Pages |
| Target Backend | ASP.NET Core Web API (.NET 8) |
| Target Database | SQL Server / Azure SQL |

---

## Project Status

This is an active **demo build** undergoing transformation into a full multi-tenant SaaS platform. The current branch structure is:

| Branch | Purpose |
|---|---|
| `master` | Stable, deployable — source for GitHub Pages |
| `Development` | Integration branch — tested features merged here before `master` |
| `First_UpgradeToSaaS_ReadyApplication` | Active feature development branch |

---

## Getting Started (Local Development)

```bash
# Restore dependencies and run locally
dotnet restore
dotnet run
```

The app will be available at `https://localhost:5001` by default.
