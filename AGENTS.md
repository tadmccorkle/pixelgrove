# Agent Development Guide

## Project Overview

Pixel Grove is a full-stack web application for sharing digital photo albums.

- **Backend**: Minimal API in C# targeting .NET 10 (`backend/`)
- **Frontend**: Single-page app using Bun, React, TypeScript, Tailwind CSS, and Shadcn/Radix UI (`webapp/`)
- **Auth**: Multi-scheme authentication (cookies, JWT bearer tokens, API keys)
- **Database**: PostgreSQL (or in-memory for development)

## Directory Structure

- `backend/` - Minimal API backend
- `webapp/` - Web app frontend

## Commands

- **Full-Stack Run**: `docker-compose up`

## Important Development Notes

1. In development, whether using docker or running standalone, the backend runs at `http://localhost:4815`.
2. **Be humble and honest.** NEVER overstate what you got done or what actually works.
