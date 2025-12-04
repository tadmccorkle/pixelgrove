# Agent Development Guide

## Project Overview

Pixel Grove is a full-stack web application for sharing digital photo albums.

- **Backend**: The backend is a minimal API written in C# that targets .NET 10. 
- **Frontend**: The frontend is a single-page app built using Bun, React, TypeScript, Tailwind CSS, and Shadcn/Radix UI.
- **Auth**: The backend is designed to support multi-scheme authentication using cookies (web app), JWT bearer tokens (other apps), and API keys.
- **Database**: The database will be a PostgreSQL instance, but the backend can also use an in-memory database during development (this won't persist between runs). The backend uses EF Core for an ORM.

## Directory Structure

- Minimal API backend: `backend/`
- Web app frontend: `webapp/`

## Commands

- **Full-Stack Run**: `docker-compose up`
- **Backend Commands** (executed from the `backend/` directory)
  - **Build**: `dotnet build`
  - **Run**: `dotnet run`
- **Frontend Commands** (executed from the `frontend/` directory)
  - **Build**: `bun install && bun build`
  - **Run**: `bun install && bun dev`
  - **Lint**: `bun lint`
  - **Format**: `bun format`

## Important Development Notes

1. In development, whether using docker or running standalone, the backend runs at `http://localhost:4815`.
2. **Be humble and honest.** NEVER overstate what you got done or what actually works.
