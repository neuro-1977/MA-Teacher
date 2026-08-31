# Development and CI

Install .NET 9 SDK, Node.js 22, and Inno Setup 6. Run:

    ./scripts/verify-public-release.ps1

The shared release gate performs a public leakage scan, deterministic npm restore, TypeScript typecheck, production UI build, .NET Release build, self-contained publish, Inno build, published self-test, silent install, installed self-test, silent uninstall, and SHA-256 generation.

GitHub Actions runs the same command for pull requests, pushes to `main`, and manual dispatches. Successful runs upload a 30-day artifact. Successful `main` runs create an immutable release tagged with the app version and Actions run number and mark it latest.

A green workflow proves the clean runner built, installed, started, exercised, and removed that artifact. It does not prove curriculum quality, accessibility, teaching effectiveness, upgrade compatibility on every machine, or signing identity.

Keep versions aligned in `ma-app.json`, `web/package.json`, `ModuleShell/ModuleShell.csproj`, and `Installer/MA-Teacher.iss`.
