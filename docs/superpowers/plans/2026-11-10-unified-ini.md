# Unified INI Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the five runtime config files with one `watcher.ini`.

**Architecture:** A `WatcherConfiguration.Load(string)` loader reads one INI file once and maps sections to existing settings records. `ConsoleRunner` consumes the mapped settings and portal URLs; `CmdArguments` exposes `ConfigFilePath`.

**Tech Stack:** C#, .NET 10, Microsoft.Extensions.Configuration.Ini, System.CommandLine, xUnit.

**Spec:** `docs/superpowers/specs/2026-11-10-unified-ini-design.md`

## Global Constraints

- Preserve section/key names and existing defaults; no new dependencies.
- Use `--config` and `-c`; replace the five legacy CLI options.
- Missing `[basic]`/`[nodejs]` sections disable the filter/scraper.
- Leave NLog and cookie data separate; do not deploy or merge the branch.

## Review Focus

- Relative config paths resolve from the working directory, including a non-default path.
- Optional sections must not accidentally enable filtering or scraping.
- Portal URLs containing equals signs and multiple URLs survive INI parsing.
- Missing files and malformed typed values fail visibly.
- Default booleans, nullable values and comma-separated lists retain their behavior.

### Task 1: Single-file loading and CLI

**Files:** `RealEstatesWatcher.UI.Console/CmdArguments.cs`, new `WatcherConfiguration.cs`, `ConsoleRunner.cs`, and the console test project.

**Interfaces:** `CmdArguments.ConfigFilePath`, `WatcherConfiguration.Load(string)` producing Engine, GoogleCloud, EmailHandler, FileHandler, BasicFilter, WebScraper and PortalUrls properties using existing settings types.

- [x] Add CLI and real-INI tests for the review focus cases and full settings mapping; observe failures against the old CLI/loader.
- [x] Replace CLI options, extract existing binding logic to the loader, and wire the console runner to it.
- [x] Run console tests; expected: all pass.

### Task 2: Templates, deployment and verification

**Files:** unified template, README, Dockerfile, console project publish items and launch settings; remove five obsolete templates.

**Interfaces:** All examples and deployment entrypoints consume `--config configs/watcher.ini` from Task 1.

- [x] Supply a documented unified template and migration example, and update deployment metadata.
- [x] Test that the template loads through the production loader and preserves defaults/optional sections.
- [x] Run the solution tests, Release publish, CLI help/missing-file smoke checks, Slopwatch and a final diff review; expected: no introduced failures or stale runtime config references.

## Execution record

- User approved INI and requested implementation. Execute in this session on `feat/320-unified-ini-config`, in the clean checkout cloned specifically for this task.
- The installed SDK is older than the repository pin. Install the pinned SDK in a temporary directory without changing `global.json`.
- Baseline solution tests passed. The new `--config` / `-c` tests failed against the old CLI; the template test failed before adding the template. Updated console tests: 18 passed. Full solution: 162 passed across 22 test projects, no failures or skips.
- Release publish passed, and a temporary safe `configs/watcher.ini` fixture was copied to publish output byte-for-byte. The temporary source fixture was removed afterward.
- Slopwatch reported no new findings. Secrets scans passed. Existing NU1507 local NuGet-source warnings and existing portal nullable-reference warnings remain.
- Independent read-only review found one Docker documentation omission: runtime configs are ignored by Docker. Corrected the documented run command to mount `watcher.ini` read-only; no other actionable findings.
- Published CLI smoke checks passed: help advertises `--config`/`-c`, and a missing config fails visibly with `FileNotFoundException` and a nonzero exit code. No watcher was run against live portals or notification services.
