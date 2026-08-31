# Development and CI

Install .NET 9 SDK, Node.js 22, and Inno Setup 6. Run:

    ./scripts/verify-public-release.ps1

The shared release gate performs a public leakage scan, deterministic npm restore, TypeScript typecheck, production UI build, .NET Release build, self-contained publish, Inno build, published self-test, silent install, installed self-test, silent uninstall, and SHA-256 generation.

## GitHub feedback ingestion

GitHub Issues and Discussions are enabled for teachers, supervised students, and school teams. Public reports must contain synthetic data only.

With MA-Teacher running and GitHub CLI authenticated, import the current issue and comment history idempotently into the local SQLite feedback queue:

```powershell
./scripts/sync-github-feedback.ps1
```

The importer reads only `neuro-1977/MA-Teacher`, accepts at most 200 issues per run, stores issue and comment fingerprints under the canonical MA-Teacher database, updates changed reports, and does not delete closed history. Read the queue at `GET http://127.0.0.1:5201/api/development/feedback?state=open`.

Before planning public-feedback work, Serenity should sync and read the open queue, inspect linked comments, separate observation from diagnosis, and leave a development breadcrumb for accepted work. A GitHub report is input, not proof: reproduce it against the current source before changing code, and never import a private security report into the public-feedback lane.

GitHub Actions runs the same command for pull requests, pushes to `main`, and manual dispatches. Successful runs upload a 30-day artifact. Successful `main` runs create an immutable release tagged with the app version and Actions run number and mark it latest.

A green workflow proves the clean runner built, installed, started, exercised, and removed that artifact. It does not prove curriculum quality, accessibility, teaching effectiveness, upgrade compatibility on every machine, or signing identity.

Keep versions aligned in `ma-app.json`, `web/package.json`, `ModuleShell/ModuleShell.csproj`, and `Installer/MA-Teacher.iss`.
