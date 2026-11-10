# Unified INI configuration

Issue: https://github.com/supermartzin/real-estates-watcher/issues/320

The approved approach combines the five runtime files into `watcher.ini`, selected with `--config` (alias `-c`). Existing section names and setting keys remain: `[general]`, `[gcloud]`, `[email]`, `[file]`, `[basic]`, and `[nodejs]`. A new `[portals]` section contains indexed URL entries (`0=https://...`, `1=https://...`), with INI comments used to disable entries.

Load the file once and map it into the existing settings records. Missing `[basic]` and `[nodejs]` sections mean no filtering and no scraper respectively; retain the existing setting defaults. Interpret the configuration file argument relative to the current working directory. Paths stored inside the configuration retain their existing working-directory semantics.

Replace the old CLI options and five templates, update publish metadata, Docker, launch profiles, README and migration instructions, and exercise real INI parsing in tests. Keep `NLog.config` and scraper cookie data separate, as agreed in the proposal.

## Approaches

- One INI file: selected by the user; reuses the current parser and setting names with a small migration.
- JSON: supports arrays and nesting directly, but requires a broader format migration and was declined.

No runtime package changes, unrelated application behavior changes, deployment, or merge into the default branch are included.
