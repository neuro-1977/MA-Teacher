# Architecture

- `web/`: React and TypeScript task-led UI built by Vite.
- `ModuleShell/`: .NET 9 WPF and WebView2 host, loopback API, and SQLite stores.
- `Installer/`: self-contained Windows publish and Inno Setup package.
- `scripts/`: leakage and end-to-end release verification.

The process serves its packaged UI on `127.0.0.1:5201`. Mutations require exact methods, loopback origin, and intent headers. This is local origin hardening, not user authentication.

SQLite is the local record authority. Attachments are database BLOBs rather than loose uploads. The packaged `--self-test` starts the real host with disposable data, checks database health, serves the built UI, checks a missing asset, and exits with a machine-readable process code.
