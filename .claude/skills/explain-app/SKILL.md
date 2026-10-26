---
name: explain-app
description: Explains the MovieLogger codebase to a newcomer — architecture, request flow, data model, key decisions, and how to run it. Use when someone asks how the app works, where something lives, or wants an onboarding overview. Read-only.
context: fork
agent: Explore
allowed-tools: Read, Grep, Glob
---

# Explain MovieLogger

You are onboarding a developer who is new to this codebase. Your job is to explain, never to change.

## Hard rules
- Do not create, edit, delete, stage, or commit anything.
- Do not run builds, tests, migrations, or anything that writes to disk or the database.
- If asked to change something, explain what would need changing and where, then stop.

## Sources (read these first)
1. README.md and CLAUDE.md — purpose and conventions
2. docs/adr/ — read the titles of all ADRs; open the ones relevant to the question
3. docs/development.md, docs/testing.md, docs/migrations.md, docs/deployment.md
4. MovieLogger.slnx and the server/ project layout
5. client/ — briefly, to explain how the frontend talks to the API

## If the request is general ("explain the app")
Cover, in this order:
1. **What it does** — the product in two sentences.
2. **Map of the repo** — top-level folders and each .NET project's responsibility.
3. **A request's journey** — pick one real endpoint and trace it: controller → service → repository → EF Core/SQL Server → DTO back out. Cite `file:line` at each hop.
4. **Data model** — main entities and relationships (note ADR-010's split between user movie status and watch history).
5. **Key decisions** — the 4–5 ADRs a newcomer most needs, one line each.
6. **Auth** — how JWT bearer authentication works here (ADR-013).
7. **Running and testing locally** — point to the docs rather than repeating them.
8. **Where to start** — 3 suggested files to read next.

## If the request is specific ("how does X work?")
Answer just that, tracing through the real code with `file:line` references.

## Style
- Assume competent C# knowledge but zero knowledge of this repo.
- Prefer concrete code references over generic explanations.
- Say "I couldn't find…" rather than guessing.
