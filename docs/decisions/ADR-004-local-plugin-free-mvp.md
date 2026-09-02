# ADR-004 — Local-Only, Plug-in-Free MVP

- **Status:** Proposed; owner recency ratification required at G0
- **Date:** 2026-09-01

## Context

A Start companion handles personal application metadata and queries. Cloud search, whole-disk crawling, plug-ins, arbitrary commands, or broad activity-history access would add privacy and execution risk without helping the core feasibility question.

## Decision

The MVP works fully offline. Search providers are installed apps, a versioned internal Settings/Control Panel catalog, and named common places. There is no web search, telemetry, plug-in loader, PowerShell integration, arbitrary command execution, browser-history access, or Windows-wide recent-document ingestion.

If ratified at G0, recency records only bounded counts and timestamps for applications launched through Ember Start. Query text, arguments, paths, app inventory, usernames, window titles, and document history are never logged.

Theme input is schema-limited data and project-owned resources; arbitrary loose XAML is not loaded.

## Consequences

- The initial product is private by architecture and useful offline.
- Local file search and any health telemetry require separate post-v1 decisions and explicit user controls.
- Diagnostics can be previewed, cleared, and remain bounded.

## Validation

Static and runtime tests must show no network requirement, arbitrary IPC command, plug-in load path, sensitive log field, or unbounded local retention.
