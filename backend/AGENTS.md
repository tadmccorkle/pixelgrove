# Backend Agent Guide

## Overview

Minimal API written in C# targeting .NET 10.

- **ORM**: Entity Framework Core
- **Database**: PostgreSQL in production; in-memory database available for development (does not persist between runs)
- **Auth**: Multi-scheme authentication using cookies (web app), JWT bearer tokens (other apps), and API keys

## Commands

All commands should be executed from this directory (`backend/`).

- **Build**: `dotnet build`
- **Run**: `dotnet run`

## Development Notes

- The backend runs at `http://localhost:4815` in development.
