# Tests

The test tree mirrors the production solution. Each implementation assembly has its own `*.Tests` project under the corresponding area:

```text
tests/
├── RealEstatesWatcher.Core.Tests/
├── RealEstatesWatcher.Models.Tests/
├── RealEstatesWatcher.UI.Console.Tests/
├── Filters/
├── Handlers/
├── Portals/
├── Scrapers/
└── Tools/
```

Common package configuration lives in `tests/Directory.Build.props`. Small source-only helpers under `tests/Common` and `tests/Portals` are linked only into projects that need them.

Run the complete test suite from the repository root:

```powershell
dotnet test "Real Estates Watcher.slnx"
```

Collect cross-platform line coverage:

```powershell
dotnet test "Real Estates Watcher.slnx" --collect:"XPlat Code Coverage"
```

## Current scope

The suite covers:

- model validation, equality, display formatting, and layout parsing;
- basic filtering, including bounds and unknown values;
- configuration-key attributes;
- HTML generation and local-file output;
- email-handler validation and skip behavior without network access;
- engine registration, validation, filtering, initial notifications, and lifecycle;
- portal-base scraping/parsing behavior, all concrete portal identities, and a representative local HTML fixture for every concrete parser;
- scraper input validation without launching Node.js;
- command-line argument parsing.

All tests are deterministic and avoid live websites, SMTP servers, timers, and child processes.

## Next additions

1. Refresh the small parser fixtures from saved, anonymized real pages when a portal changes markup; add alternate fixtures for missing prices, images, and optional fields.
2. Extract an SMTP client interface from the email handler, then test message recipients, subject, HTML body, TLS choice, authentication, cancellation, and error wrapping.
3. Extract a process-runner interface from the Node.js scraper, then test argument passing, stdout/stderr handling, timeout cancellation, process-tree termination, and exit codes.
4. Replace the engine timer with an injectable clock/periodic scheduler, then test subsequent polling, new-post deduplication, handler isolation, cancellation, and stop/check races without waiting in real time.
5. Move portal selection and INI binding out of `ConsoleRunner` into injectable services so registration and configuration parsing can be tested independently.

Regression tests verify that an empty initial snapshot still starts periodic checks and that empty addresses leave no unresolved tokens in generated HTML.
